using System;

namespace WeakEvents.Fody.Test
{
#pragma warning disable 0067

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class UnsupportedEventDelegateType
    {
        public delegate void NotDerivedFromEventArgs(object o, int i /* Not derived from EventArgs */);

        public event NotDerivedFromEventArgs EventNotDerivedFromEventArgs;

        public delegate void TooFewArgs(object o /* Too few args */);

        public event TooFewArgs EventTooFewArgs;

        public delegate void TooManyArgs(object o1, EventArgs args1, EventArgs arg2 /* Too many args */);

        public event TooManyArgs EventTooManyArgs;

        public delegate void FirstParamNotObject(int i /* Not System.Object */, EventArgs args);

        public event FirstParamNotObject EventFirstParamNotObject;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class AwesomeEventArgs : EventArgs
    {
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class SupportedEventDelegateType
    {
        public event EventHandler BasicEvent;

        public event System.ComponentModel.PropertyChangedEventHandler SpecializedEvent;

        public event EventHandler<AssemblyLoadEventArgs> GenericEvent;

        public event EventHandler<AwesomeEventArgs> GenericCustomArgsEvent;

        public delegate void CustomEventHandler(object sender, AwesomeEventArgs args);

        public event CustomEventHandler CustomEvent;

        public static event EventHandler<AssemblyLoadEventArgs> StaticEvent;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class EventWithUnknownBackingDelegate
    {
        private EventHandler _eh;
        private EventHandler _eh2;
        private static EventHandler _staticEh;

        public event EventHandler EventWithNoBackingDelegate
        {
            add { }
            remove { }
        }

        public event EventHandler EventNotUsedInRemove
        {
            add { _eh += value; }
            remove { }
        }

        public event EventHandler EventNotUsedInAdd
        {
            add { }
            remove { _eh -= value; }
        }

        public event EventHandler MultipleEventDelegates
        {
            add { _eh += value; _eh2 += value; }
            remove { _eh -= value; _eh2 += value; }
        }

        public event EventHandler InstanceEventAddUsesStaticDelegate
        {
            add { _staticEh += value; }
            remove { _eh -= value; }
        }

        public event EventHandler InstanceEventRemoveUsesStaticDelegate
        {
            add { _eh += value; }
            remove { _staticEh -= value; }
        }
    }

#pragma warning restore 0067
}