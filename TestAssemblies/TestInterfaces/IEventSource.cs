using System;
using System.Collections.Specialized;

namespace TestInterfaces
{
    public delegate void CannotBeMadeWeak(object sender, int args/* Not derived from EventArgs */);

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        event CannotBeMadeWeak CannotBeMadeWeak;

        void FireCannotBeMadeWeak();

        void SubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler);

        void UnsubscribeStaticEvent(EventHandler<AssemblyLoadEventArgs> handler);

        void FireStaticGenericEvent();

        bool IsStaticGenericEventSubscribed { get; }
    }

#pragma warning restore 0067
}