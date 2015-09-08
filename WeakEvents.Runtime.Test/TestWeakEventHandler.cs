using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeakEvents.Runtime.Test
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class TestWeakEventHandler
    {
        private class Stub1
        {
            public int FireCount;

            public void Handler(object sender, EventArgs args)
            {
                ++FireCount;
            }

            public void Handler2(object sender, EventArgs args)
            {
            }
        }

        private class Stub2
        {
            public void Handler(object sender, EventArgs args)
            {
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_WrongDeclaringType_Throws()
        {
            new WeakEventHandler<Stub1, EventArgs>(new Stub2().Handler /* Correct event handler delgate type, but wrong class type */, handler => { });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullUnregister_Throws()
        {
            new WeakEventHandler<Stub1, EventArgs>(new Stub1().Handler, null);
        }

        [TestMethod]
        public void Handler_Invoke_CallsTarget()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { }).HandlerT;
            handler(this, new EventArgs());
            Assert.AreEqual(1, target.FireCount);
        }

        [TestMethod]
        public void Handler_Invoke_TargetGC_CallsUnregister()
        {
            bool unregister = false;
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { unregister = true; }).HandlerT;
            handler(this, new EventArgs());
            Assert.AreEqual(1, target.FireCount);

            target = null;
            System.GC.Collect();

            handler(this, new EventArgs());
            Assert.IsTrue(unregister);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsHandlerFor_Null_Throws()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { });

            Assert.IsFalse(handler.IsHandlerFor(null));
        }

        [TestMethod]
        public void IsHandlerFor_TargetGC_False()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { });

            target = null;
            System.GC.Collect();

            Assert.IsFalse(handler.IsHandlerFor(new Stub1().Handler));
        }

        [TestMethod]
        public void IsHandlerFor_WrongObject_False()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { });
            Assert.IsFalse(handler.IsHandlerFor(new Stub1().Handler));
        }

        [TestMethod]
        public void IsHandlerFor_WrongHandler_False()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { });
            Assert.IsFalse(handler.IsHandlerFor(target.Handler2));
        }

        [TestMethod]
        public void IsHandlerFor_CorrectHandler_True()
        {
            var target = new Stub1();
            var handler = new WeakEventHandler<Stub1, EventArgs>(target.Handler, eh => { });
            Assert.IsTrue(handler.IsHandlerFor(target.Handler));
        }
    }
}