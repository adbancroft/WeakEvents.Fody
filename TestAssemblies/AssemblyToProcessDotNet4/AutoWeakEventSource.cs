using System;
using System.Dynamic;
using WeakEvents;

namespace AssemblyToProcessDotNet4
{
#pragma warning disable 0067

    // The class which we weave to implement weak events
    [ImplementWeakEvents]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AutoWeakEventSource : TestInterfaces.IEventSource
    {
        public event EventHandler BasicEvent;
        public void FireBasicEvent()
        {
            if (BasicEvent!=null)
                BasicEvent(this, new EventArgs());
        }
        public bool IsBasicEventSubscribed { get { return BasicEvent != null; } }

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler NonGenericEvent;
        public void FireNonGenericEvent()
        {
            if (NonGenericEvent != null)
                NonGenericEvent(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public bool IsNonGenericEventSubscribed { get { return NonGenericEvent != null; } }

        public event EventHandler<AssemblyLoadEventArgs> GenericEvent;
        public void FireGenericEvent()
        {
            if (GenericEvent != null)
                GenericEvent(this, new AssemblyLoadEventArgs(null));
        }
        public bool IsGenericEventSubscribed { get { return GenericEvent != null; } }

        // The weaver should ignore this as the delegate doesn't conform to the standard (object, EventArgs) pattern
        public event TestInterfaces.CannotBeMadeWeak CannotBeMadeWeak;
        public void FireCannotBeMadeWeak()
        {
            if (CannotBeMadeWeak != null)
                CannotBeMadeWeak(this, 10);
        }

        public static event EventHandler<AssemblyLoadEventArgs> StaticGenericEvent;
        public void SubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler)
        {
            StaticGenericEvent += handler;
        }
        public void UnsubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler)
        {
            StaticGenericEvent -= handler;
        }
        public void FireStaticGenericEvent()
        {
            if (StaticGenericEvent != null)
                StaticGenericEvent(null, new AssemblyLoadEventArgs(null));
        }
        public bool IsStaticGenericEventSubscribed { get { return StaticGenericEvent != null; } }
    }
}
