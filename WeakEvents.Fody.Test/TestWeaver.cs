using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestInterfaces;

namespace WeakEvents.Fody.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TestWeaver
    {
        private static WeaverHelper _weaverHelper;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        class EventTarget
        {
            public int FireCount;
            public void EventHandler(object o, EventArgs args)
            {
                ++FireCount;
            }
            public void EventHandler(object o, int args)
            {
                ++FireCount;
            }
        }

        [AssemblyInitialize]
        public static void Setup(TestContext context)
        {
            _weaverHelper = new WeaverHelper("AssemblyToProcessDotNet4.dll");
        }

        private IEventSource CreateOriginalEventSource()
        {
            return new AssemblyToProcessDotNet4.AutoWeakEventSource();
        }

        private IEventSource CreateWovenEventSource()
        {
            return (IEventSource)_weaverHelper.GetInstance("AssemblyToProcessDotNet4.AutoWeakEventSource");
        }

        private void TestWeakSubscribe(Action<EventTarget, IEventSource> subscribe, Action<IEventSource> fireEvent)
        {
            // Setup event source, target & wire together
            var target = new EventTarget();
            var wr = new WeakReference(target);
            var source = CreateWovenEventSource();
            subscribe(target, source);

            // Confirm event fires correctly.
            Assert.IsNotNull(wr.Target);
            fireEvent(source);
            Assert.AreEqual(1, target.FireCount);

            // Confirm that the event source does not keep the target alive.
            target = null;
            System.GC.Collect();
            Assert.IsNull(wr.Target);
        
            // Fire the event again to force clean up of the weak event handler instance
            // There is no way to check that clean up, however this will confirm the woven IL is correct
            fireEvent(source);
            
            // Keep the source alive until here: if it's GC'd this test will be invalid.
            System.GC.KeepAlive(source);
        }

        private void TestWeakUnsubscribe(Action<EventTarget, IEventSource> subscribe, Action<IEventSource> fireEvent, Action<EventTarget, IEventSource> unsubscribe, Func<IEventSource, bool> isEventSubscribed)
        {
            // Setup event source, target & wire together
            var target = new EventTarget();
            var wr = new WeakReference(target);

            var source = CreateWovenEventSource();
            subscribe(target, source);

            // Confirm event fires correctly.
            Assert.IsNotNull(wr.Target);
            fireEvent(source);
            Assert.AreEqual(1, target.FireCount);

            // Remove the handler
            unsubscribe(target, source);

            // Fire the event again & confirm handler removed
            fireEvent(source);
            Assert.IsFalse(isEventSubscribed(source));
            Assert.AreEqual(1, target.FireCount);

            // Keep the source alive until here: if it's GC'd this test will be invalid.
            System.GC.KeepAlive(source);
        }

        private void TestStrongSubscribe(IEventSource source, Action<EventTarget, IEventSource> subscribe, Action<IEventSource> fireEvent)
        {
            // Confirm that an event that cannot be makde weak retains
            // a strong reference to the target and still works.

            // Setup event source, target & wire together
            var target = new EventTarget();
            var wr = new WeakReference(target);
            subscribe(target, source);

            // Confirm event fires correctly.
            Assert.IsNotNull(wr.Target);
            fireEvent(source);
            Assert.AreEqual(1, target.FireCount);

            // Confirm that the event source does keep the target alive.
            target = null;
            System.GC.Collect();
            Assert.IsNotNull(wr.Target);

            fireEvent(source);
            target = (EventTarget)wr.Target;
            Assert.AreEqual(2, target.FireCount);
        }

        [TestMethod]
        public void Test_BasicEvent_Subscribe_IsWeak()
        {
            TestWeakSubscribe(
                (target, source) => source.BasicEvent += target.EventHandler,
                source => source.FireBasicEvent()
            );
            TestStrongSubscribe(CreateOriginalEventSource(),
                (target, source) => source.BasicEvent += target.EventHandler,
                source => source.FireBasicEvent());
        }

        [TestMethod]
        public void Test_BasicEvent_Unsubscribe_IsWeak()
        {
            TestWeakUnsubscribe(
                (target, source) => source.BasicEvent += target.EventHandler,
                source => source.FireBasicEvent(),
                (target, source) => source.BasicEvent -= target.EventHandler,
                source => source.IsBasicEventSubscribed
            );
        }
        
        [TestMethod]
        public void Test_NonGenericEvent_Subscribe_IsWeak()
        {
            TestWeakSubscribe(
                (target, source) => source.NonGenericEvent += target.EventHandler,
                source => source.FireNonGenericEvent()
            );
            TestStrongSubscribe(CreateOriginalEventSource(),
                (target, source) => source.NonGenericEvent += target.EventHandler,
                source => source.FireNonGenericEvent()
            );
        }

        [TestMethod]
        public void Test_NonGenericEvent_Unsubscribe_IsWeak()
        {
            TestWeakUnsubscribe(
                (target, source) => source.NonGenericEvent += target.EventHandler,
                source => source.FireNonGenericEvent(),
                (target, source) => source.NonGenericEvent -= target.EventHandler,
                source => source.IsNonGenericEventSubscribed
            );
        }

        [TestMethod]
        public void Test_GenericEvent_Subscribe_IsWeak()
        {
            TestWeakSubscribe(
                (target, source) => source.GenericEvent += target.EventHandler,
                source => source.FireGenericEvent()
            );
            TestStrongSubscribe(CreateOriginalEventSource(),
                (target, source) => source.GenericEvent += target.EventHandler,
                source => source.FireGenericEvent()
            );
        }

        [TestMethod]
        public void Test_GenericEvent_Unsubscribe_IsWeak()
        {
            TestWeakUnsubscribe(
                (target, source) => source.GenericEvent += target.EventHandler,
                source => source.FireGenericEvent(),
                (target, source) => source.GenericEvent -= target.EventHandler,
                source => source.IsGenericEventSubscribed
            );
        }

        [TestMethod]
        public void StaticEvent_Subscribe_IsWeak()
        {
            TestWeakSubscribe(
                (target, source) => source.SubscribeStaticEvent(target.EventHandler),
                source => source.FireStaticGenericEvent()
            );
            TestStrongSubscribe(CreateOriginalEventSource(),
                (target, source) => source.SubscribeStaticEvent(target.EventHandler),
                source => source.FireStaticGenericEvent()
            );
        }

        [TestMethod]
        public void StaticEvent_Unsubscribe_IsWeak()
        {
            TestWeakUnsubscribe(
                (target, source) => source.SubscribeStaticEvent(target.EventHandler),
                source => source.FireStaticGenericEvent(),
                (target, source) => source.UnsubscribeStaticEvent(target.EventHandler),
                source => source.IsStaticGenericEventSubscribed
            );
        }

        [TestMethod]
        public void Test_CannotBeMadeWeak_Subscribe_IsStrong()
        {
            TestStrongSubscribe(
                CreateWovenEventSource(),
                (target, source) => source.CannotBeMadeWeak += target.EventHandler,
                source => source.FireCannotBeMadeWeak());
        }

        [TestMethod]
        public void Test_WovenClass_WeakEventAttributeRemoved()
        {
            IEventSource wovenClass = CreateWovenEventSource();

            Assert.AreEqual(0, wovenClass.GetType().GetCustomAttributes(typeof(WeakEvents.ImplementWeakEventsAttribute), false).Length);
        }

        [TestMethod]
        public void Test_WovenAssembly_WeakEventReferenceRemoved()
        {
            IEventSource wovenClass = CreateWovenEventSource();

            Assert.IsFalse(wovenClass.GetType().Assembly.GetReferencedAssemblies().Any(name => name.FullName.Equals(typeof(WeakEvents.ImplementWeakEventsAttribute).Assembly.FullName)));
        }
    }
}
