using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WeakEvents.Runtime.Test
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class TestDelegateConvert
    {
        [TestMethod]
        public void ChangeType_Null_Null()
        {
            Assert.IsNull(DelegateConvert.ChangeType<object>(null));
        }

        [TestMethod]
        public void ChangeType_SingleCast_SingleCast()
        {
            int invokeCounter = 0;
            EventHandler d = (object o, EventArgs args) => { ++invokeCounter; };

            EventHandler<EventArgs> dt = DelegateConvert.ChangeType<EventHandler<EventArgs>>(d);

            Assert.IsNotNull(dt);
            Assert.AreNotSame(d, dt);
            d(this, new EventArgs());
            Assert.AreEqual(1, invokeCounter);
            dt(this, new EventArgs());
            Assert.AreEqual(2, invokeCounter);
        }

        [TestMethod]
        public void ChangeType_MultiCast_MultiCast()
        {
            int invokeCounter = 0;
            EventHandler d = (object o, EventArgs args) => { ++invokeCounter; };
            d += (object o, EventArgs args) => { ++invokeCounter; };

            EventHandler<EventArgs> dt = DelegateConvert.ChangeType<EventHandler<EventArgs>>(d);

            Assert.IsNotNull(dt);
            Assert.AreNotSame(d, dt);
            d(this, new EventArgs());
            Assert.AreEqual(2, invokeCounter);
            dt(this, new EventArgs());
            Assert.AreEqual(4, invokeCounter);
        }

        [TestMethod]
        public void ChangeType_SameType_ReturnsOriginalInstance()
        {
            int invokeCounter = 0;
            EventHandler<EventArgs> d = (object o, EventArgs args) => { ++invokeCounter; };

            EventHandler<EventArgs> dt = DelegateConvert.ChangeType<EventHandler<EventArgs>>(d);

            Assert.IsNotNull(dt);
            Assert.AreSame(d, dt);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void ChangeType_IncompatibleType_Throws()
        {
            int invokeCounter = 0;
            EventHandler<AssemblyLoadEventArgs> d = (object o, AssemblyLoadEventArgs args) => { ++invokeCounter; };

            EventHandler<ResolveEventArgs> dt = DelegateConvert.ChangeType<EventHandler<ResolveEventArgs>>(d);
        }

        [TestMethod]
        public void ChangeType_DerivedArgs_IsCallable()
        {
            int invokeCounter = 0;
            EventHandler<EventArgs> d = (object o, EventArgs args) => { ++invokeCounter; };

            EventHandler<AssemblyLoadEventArgs> dt = DelegateConvert.ChangeType<EventHandler<AssemblyLoadEventArgs>>(d);

            Assert.IsNotNull(dt);
            Assert.AreNotSame(d, dt);
            d(this, new EventArgs());
            Assert.AreEqual(1, invokeCounter);
            dt(this, new AssemblyLoadEventArgs(null));
            Assert.AreEqual(2, invokeCounter);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ChangeType_NullType_Throws()
        {
            int invokeCounter = 0;
            EventHandler d = (object o, EventArgs args) => { ++invokeCounter; };

            DelegateConvert.ChangeType(d, (Type)null);
        }
    }
}