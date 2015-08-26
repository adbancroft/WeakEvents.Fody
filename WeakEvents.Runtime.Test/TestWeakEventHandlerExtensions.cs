using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WeakEvents.Runtime.Test
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class TestWeakEventHandlerExtensions
    {
        class Stub1
        {
            public EventHandler<EventArgs> StubEvent;

            public int FireCount;
            public void Handler(object sender, EventArgs args) { ++FireCount; }
        }

        private static void StaticHandler(object o, EventArgs args)
        {
        }

        #region Makeweak<T> - EventHandler<EventArgs>

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MakeWeakT_NullEvent_Throws()
        {
            WeakEventHandlerExtensions.MakeWeak<EventArgs>(null, args => { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MakeWeakT_NullUnregister_Throws()
        {
            var s1 = new Stub1();
            WeakEventHandlerExtensions.MakeWeak<EventArgs>(s1.Handler, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MakeWeakT_StaticHandler_Throws()
        {
            WeakEventHandlerExtensions.MakeWeak<EventArgs>(StaticHandler, args => { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MakeWeakT_AnonymousMethod_Throws()
        {
            int counter = 0; // We have to capture something in order to create a closure that the called code can detec.
            WeakEventHandlerExtensions.MakeWeak<EventArgs>((o, args) => ++counter, args => { });
        }

        [TestMethod]
        public void MakeWeakT_AlreadyWeak_ReturnsExisting()
        {
            var s1 = new Stub1();
            var weakEventHandler1 = WeakEventHandlerExtensions.MakeWeak<EventArgs>(s1.Handler, args => { });
            var weakEventHandler2 = WeakEventHandlerExtensions.MakeWeak<EventArgs>(weakEventHandler1, args => { });

            Assert.AreSame(weakEventHandler1, weakEventHandler2);
        }

        [TestMethod]
        public void MakeWeakT_DoesntPreventGC()
        {
            var s1 = new Stub1();
            WeakReference ws1 = new WeakReference(s1);
            var weakEventHandler1 = WeakEventHandlerExtensions.MakeWeak<EventArgs>(s1.Handler, args => { });
            s1 = null;
            GC.Collect();
            Assert.IsFalse(ws1.IsAlive);
        }

        [TestMethod]
        public void MakeWeakT_FireEvent_SourceHandlerCalled()
        {
            var s1 = new Stub1();
            var weakEventHandler = WeakEventHandlerExtensions.MakeWeak<EventArgs>(s1.Handler, args => { });
            weakEventHandler(this, new EventArgs());
            Assert.AreEqual(1, s1.FireCount);
        }

        #endregion

        #region FindWeak<T> - EventHandler<EventArgs>

        [TestMethod]
        public void FindWeakT_NullDelegate_ReturnsHandler()
        { 
            var s1 = new Stub1();
            EventHandler<EventArgs> handler = s1.Handler;
            Assert.IsNull(s1.StubEvent);
            Assert.AreSame(handler, WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, handler));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FindWeakT_NullHandler_Throws()
        {
            var s1 = new Stub1();
            WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, null);
        }

        [TestMethod]
        public void FindWeakT_NotWeak_ReturnsStrongHandler()
        {
            var s1 = new Stub1();
            EventHandler<EventArgs> handler = s1.Handler;
            s1.StubEvent += handler;
            Assert.AreSame(handler, WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, handler));
        }

        [TestMethod]
        public void FindWeakT_NotSubscribed_ReturnsStrongHandler()
        {
            var s1 = new Stub1();
            s1.StubEvent += (o, args) => { }; // Make s1.StubEvent!=null
            EventHandler<EventArgs> handler = s1.Handler;
            Assert.AreSame(handler, WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, handler));
        }

        [TestMethod]
        public void FindWeakT_StaticHandler_ReturnsStaticHandler()
        {
            var s1 = new Stub1();
            s1.StubEvent += (o, args) => { }; // Make s1.StubEvent!=null
            EventHandler<EventArgs> handler = StaticHandler;
            Assert.AreSame(handler, WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, handler));
        }

        class WeakEventHandlerMock<TArgs> : IWeakEventHandler<TArgs>
            where TArgs : EventArgs
        {
            protected void Invoke(object sender, TArgs args) { }

            public EventHandler<TArgs> HandlerT { get { return Invoke; } }

            public EventHandler<TArgs> LastIsHandlerFor { get; private set; }

            public bool IsHandlerFor(EventHandler<TArgs> strongEventHandler)
            {
                LastIsHandlerFor = strongEventHandler;
                return true;
            }
        }

        [TestMethod]
        public void FindWeakT_IsWeak_CallsIsHandlerFor()
        {
            var weakMock = new WeakEventHandlerMock<EventArgs>();
            var s1 = new Stub1();
            s1.StubEvent += weakMock.HandlerT;
            EventHandler<EventArgs> strongHandler = s1.Handler;
            EventHandler<EventArgs> weakHandler = WeakEventHandlerExtensions.FindWeak<EventArgs>(s1.StubEvent, strongHandler);
            Assert.AreEqual(weakMock.HandlerT, weakHandler);
            Assert.AreSame(weakMock.LastIsHandlerFor, strongHandler);
        }

        #endregion
    }
}
