using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace TestInterfaces
{
    public delegate void CannotBeMadeWeak(object o, int i /* Not derived from EventArgs */);

    // Used by unit tests to access a weaved version of a class.
    public interface IEventSource
    {
        event EventHandler BasicEvent; // Equivalent of EventHandler<EventArgs>
        void FireBasicEvent();
        bool IsBasicEventSubscribed { get; }

        event NotifyCollectionChangedEventHandler NonGenericEvent; // Equivalent of EventHandler<NotifyCollectionChangedEventArgs>
        void FireNonGenericEvent();
        bool IsNonGenericEventSubscribed { get; }

        event EventHandler<AssemblyLoadEventArgs> GenericEvent;
        void FireGenericEvent();
        bool IsGenericEventSubscribed { get; }

        // The weaver should ignore this as the delegate doesn't conform to the standard (object, EventArgs) pattern
        event CannotBeMadeWeak CannotBeMadeWeak;
        void FireCannotBeMadeWeak();

        void SubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler);
        void UnsubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler);
        void FireStaticGenericEvent();
        bool IsStaticGenericEventSubscribed { get; }
    }

#pragma warning restore 0067
}
