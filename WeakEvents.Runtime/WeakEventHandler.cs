using System;
using System.Diagnostics.CodeAnalysis;

namespace WeakEvents.Runtime
{
    /// <summary>
    /// An interface to help us internally work with weak event instances.
    /// </summary>
    /// <typeparam name="TEventArgs"></typeparam>
    internal interface IWeakEventHandler<TEventArgs> // : EventHandler<TEventArgs> <-- not possible :-(
      where TEventArgs : EventArgs
    {
        /// <summary>
        /// The weak event handler itself (since we can't inherit from EventHandler&lt;TEventArgs&gt;)
        /// </summary>
        EventHandler<TEventArgs> HandlerT { get; }

        /// <summary>
        /// Test if this instance is a weak event handler for the parameter
        /// </summary>
        /// <param name="strongEventHandler">A strong event handler</param>
        /// <returns><code>true</code> if this instance is an equivalent weak event handler;<code>false</code> otherwise.</returns>
        bool IsHandlerFor(EventHandler<TEventArgs> strongEventHandler);
    }

    internal class WeakEventHandler<TSubscriber, TEventArgs> : IWeakEventHandler<TEventArgs>
        where TSubscriber : class // We need the event handler declaring type in order to correctly declare the open event handler.
        where TEventArgs : EventArgs
    {
        // This is the key to this weak event implementation: an open delegate.
        private delegate void OpenEventHandler(TSubscriber thisInstance, object sender, TEventArgs e);

        // The open delegate and the WEAK reference which we will use to invoke the open delegate.
        private readonly OpenEventHandler _openHandler;
#if NET45
        private readonly WeakReference<TSubscriber> _target; // Appears to be slighty faster than non-generic WeakReference
#else
        private readonly WeakReference _target; 
#endif

        // Used to unsubscribe THIS INSTANCE from the source event
        private Action<EventHandler<TEventArgs>> _unregister;

        public WeakEventHandler(EventHandler<TEventArgs> strongEventHandler, Action<EventHandler<TEventArgs>> unregister)
        {
            if (unregister==null)
            {
                throw new ArgumentNullException("unregister");
            }
            if (!(strongEventHandler.Target is TSubscriber))
            {
                throw new ArgumentException("Expected event handler declaring type to be " + typeof(TSubscriber) + ", got " + strongEventHandler.Target.GetType(), "strongEventHandler");
            }

#if NET45
            _target = new WeakReference<TSubscriber>((TSubscriber)strongEventHandler.Target);
#else
            _target = new WeakReference(strongEventHandler.Target);
#endif
            _openHandler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, strongEventHandler.Method);
            _unregister = unregister;
        }

        /// <summary>
        /// The weak event handler itself (since we can't inherit from EventHandler&lt;TEventArgs&gt;)
        /// </summary>
        public EventHandler<TEventArgs> HandlerT
        {
            get { return Invoke; }
        }

        // This is the weak event handler that the source event calls. 
        // We simply forward the call to the real event handler if it hasn't been GC'd.
        private void Invoke(object sender, TEventArgs e)
        {
            // Obtain a strong reference to the target instance
            TSubscriber target;
            if (TryGetTarget(out target))
                // Call the target event handler
                _openHandler(target, sender, e);
            // No strong reference, so the target must have been GC'd.
            else if (_unregister != null)
            {
                // At this point this WeakEventHandler<,> instance is now only kept alive by a strong 
                // reference from the event handler. So unregister this instance from the event handler
                // so it can be garbage colleced.
                //
                // This implies that if the event is never invoked there will be a small memory leak due to 
                // dangling WeakEventHandler<,> instance(s).
                _unregister(Invoke);
                _unregister = null;
            }
        }

        /// <summary>
        /// Test if this instance is a weak event handler for the parameter
        /// </summary>
        /// <param name="strongEventHandler">A strong event handler</param>
        /// <returns><code>true</code> if this instance is an equivalent weak event handler;<code>false</code> otherwise.</returns>
        public bool IsHandlerFor(EventHandler<TEventArgs> strongEventHandler)
        {
            if (strongEventHandler==null)
            {
                throw new ArgumentNullException("strongEventHandler");
            }

            // Get a strong reference to the target
            TSubscriber target;
            return TryGetTarget(out target)
                // Same strong target
                && ReferenceEquals(target, strongEventHandler.Target)
                // Same method
                && _openHandler.Method.Equals(strongEventHandler.Method);
        }

        private bool TryGetTarget(out TSubscriber target)
        {
#if NET45
            return _target.TryGetTarget(out target)
#else
            target = _target.Target as TSubscriber;
            return target != null;
#endif
        }
    }
}