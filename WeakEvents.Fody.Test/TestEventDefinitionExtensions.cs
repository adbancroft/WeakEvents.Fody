using System;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using WeakEvents.Fody;

namespace WeakEvents.Fody.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TestEventDefinitionExtensions
    {
        private TypeDefinition _badTypeIsValidEventDelegate;
        private TypeDefinition _goodTypeIsValidEventDelegate;
        private TypeDefinition _eventWithUnknownBackingDelegate;

        [TestInitialize]
        public void LoadTypes()
        {
            var moduleDef = ModuleDefinition.ReadModule(this.GetType().Assembly.Location, new ReaderParameters
            {
            });
            _badTypeIsValidEventDelegate = moduleDef.Types.Single(t => t.FullName.Equals(typeof(UnsupportedEventDelegateType).FullName));
            _goodTypeIsValidEventDelegate = moduleDef.Types.Single(t => t.FullName.Equals(typeof(SupportedEventDelegateType).FullName));
            _eventWithUnknownBackingDelegate = moduleDef.Types.Single(t => t.FullName.Equals(typeof(EventWithUnknownBackingDelegate).FullName));
        }

        private EventDefinition FindEvent(TypeDefinition typeDef, string eventName)
        {
            return typeDef.Events.Single(f => f.Name.Equals(eventName));
        }

        #region IsValidEventDelegate

        [TestMethod]
        public void IsValidEventDelegate_Wrong2ndArgType_False()
        {
            var eventRef = FindEvent(_badTypeIsValidEventDelegate, "EventNotDerivedFromEventArgs");

            Assert.IsFalse(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_TooFewArgs_False()
        {
            var eventRef = FindEvent(_badTypeIsValidEventDelegate, "EventTooFewArgs");

            Assert.IsFalse(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_TooManyArgs_False()
        {
            var eventRef = FindEvent(_badTypeIsValidEventDelegate, "EventTooManyArgs");

            Assert.IsFalse(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_Wrong1stArgType_False()
        {
            var eventRef = FindEvent(_badTypeIsValidEventDelegate, "EventFirstParamNotObject");

            Assert.IsFalse(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_BasicEvent_True()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "BasicEvent");

            Assert.IsTrue(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_SpecializedEvent_True()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "SpecializedEvent");

            Assert.IsTrue(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }
        
        [TestMethod]
        public void IsValidEventDelegate_GenericEvent_True()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "GenericEvent");

            Assert.IsTrue(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_CustomEvent_True()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "CustomEvent");

            Assert.IsTrue(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        [TestMethod]
        public void IsValidEventDelegate_GenericCustomArgsEvent_True()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "GenericCustomArgsEvent");

            Assert.IsTrue(EventDefinitionExtensions.IsValidEventDelegate(eventRef.EventType));
        }

        #endregion

        #region GetEventDelegate

        [TestMethod]
        public void GetEventDelegate_NoDelegate_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "EventWithNoBackingDelegate");

             Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));            
        }

        [TestMethod]
        public void GetEventDelegate_NotUsedInRemove_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "EventNotUsedInRemove");

            Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_NotUsedInAdd_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "EventNotUsedInAdd");

            Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_TooManyDelegates_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "MultipleEventDelegates");

            Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_InstanceEventAddUsesStaticDelegate_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "InstanceEventAddUsesStaticDelegate");

            Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_InstanceEventRemoveUsesStaticDelegate_Null()
        {
            var eventRef = FindEvent(_eventWithUnknownBackingDelegate, "InstanceEventRemoveUsesStaticDelegate");

            Assert.IsNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_EventHandler_NotNull()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "BasicEvent");

            Assert.IsNotNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_PropertyChangedEventHandler_NotNull()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "SpecializedEvent");

            Assert.IsNotNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_EventHandlerT_NotNull()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "GenericEvent");

            Assert.IsNotNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        [TestMethod]
        public void GetEventDelegate_GenericCustomArgsEvent_NotNull()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "GenericCustomArgsEvent");

            Assert.IsNotNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }
        
        [TestMethod]
        public void GetEventDelegate_StaticEventHandlerT_NotNull()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "StaticEvent");

            Assert.IsNotNull(EventDefinitionExtensions.GetEventDelegate(eventRef));
        }

        #endregion

        #region GetEventArgsType

        [TestMethod]
        public void GetEventArgsType_EventHandler_EventArgs()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "BasicEvent");

            Assert.AreEqual(typeof(System.EventArgs).FullName, EventDefinitionExtensions.GetEventArgsType(eventRef.EventType).FullName);
        }

        [TestMethod]
        public void GetEventArgsType_PropertyChangedEventHandler_PropertyChangedEventArgs()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "SpecializedEvent");

            Assert.AreEqual(typeof(System.ComponentModel.PropertyChangedEventArgs).FullName, EventDefinitionExtensions.GetEventArgsType(eventRef.EventType).FullName);
        }

        [TestMethod]
        public void GetEventArgsType_EventHandlerT_AssemblyLoadEventArgs()
        {
            var eventRef = FindEvent(_goodTypeIsValidEventDelegate, "GenericEvent");

            Assert.AreEqual(typeof(System.AssemblyLoadEventArgs).FullName, EventDefinitionExtensions.GetEventArgsType(eventRef.EventType).FullName);
        }

        #endregion
    }
}
