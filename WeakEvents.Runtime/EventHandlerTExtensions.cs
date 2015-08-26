using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WeakEvents.Runtime
{
    /// <summary>
    /// Extension for returning a Weak EventHandler.  This is used in conjuction with WeakEventHandler.
    /// 
    /// Based on "Solving the Problem with Events: Weak Event Handlers" by Dustin Campbell, 
    /// (see http://diditwith.net/PermaLink,guid,aacdb8ae-7baa-4423-a953-c18c1c7940ab.aspx)
    /// and licensed under Creative Commons Attribution 3.0 United States http://creativecommons.org/licenses/by/3.0/us/
    /// </summary>
    public static class WeakEventHandlerExtensions
    {
        /// <summary>
        /// Makes a weak event handler. Only works with instance methods (will not work for anonymous or static methods)
        /// </summary>
        /// <typeparam name="TCustomArgs">The type of the event args (derived from EventArgs)</typeparam>
        /// <param name="eventHandler">The event handler to make weak.</param>
        /// <param name="unregister">The callback so the weak event handler can detach from the source event.</param>
        /// <returns>A weak event handler that wraps the event handler parameter</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public static EventHandler<TCustomArgs> MakeWeak<TCustomArgs>(this EventHandler<TCustomArgs> eventHandler, Action<EventHandler<TCustomArgs>> unregister)
          where TCustomArgs : EventArgs
        {
            if (eventHandler == null)
            {
                throw new ArgumentNullException("eventHandler");
            }
            if (unregister == null)
            {
                throw new ArgumentNullException("unregister");
            }
            if (!IsInstanceMethod(eventHandler))
            {
                throw new ArgumentException("Only instance methods are supported.", "eventHandler");
            }
            // We cannot make anonymous methods (including lambdas) into weak event handlers. Doing so could potentially
            // allow the anonymous method to be GC'd, since the only thing referring to it would be the weak reference.
            // This would result in silent failures, as the event handler (the anonymous method) would no longer exist & 
            // therefore never be called. In addition, this would happen somewhat randomly as it's dependent on GC timing.
            if (IsAnonymousMethod(eventHandler))
            {
                throw new ArgumentException("Cannot create weak event to anonymous method with closure.", "eventHandler");
            }
            // Check to see if we're already weak
            if (WeakEventHandlerHelpersT<TCustomArgs>.IsWeakEventHandler(eventHandler))
            {
                return eventHandler;
            }

            IWeakEventHandler<TCustomArgs> weh = WeakEventHandlerFactory<TCustomArgs>.Create(eventHandler, unregister);
            return weh.HandlerT;
        }

        /// <summary>
        /// Finds the weak event handler that has been used to wrap the strong event handler for the supplied delegate
        /// </summary>
        /// <typeparam name="TCustomArgs">The type of the event args (derived from EventArgs)</typeparam>
        /// <param name="sourceEvent">The source event</param>
        /// <param name="strongEventHandler">The strong event handler to search for</param>
        /// <returns>The weak event handler if one exists; the strong event handler otherwise</returns>
        public static EventHandler<TCustomArgs> FindWeak<TCustomArgs>(this Delegate sourceEvent, EventHandler<TCustomArgs> strongEventHandler)
            where TCustomArgs : EventArgs
        {
            // A null source event is legitimate: when unsubscribing from an event, callers do not check if
            // the event is null. I.e. the following is normal practice.
            //  class Dog
            //  {
            //      public EventHandler<EventArgs> Barked;
            //  }
            //  var d = new Dog()
            //  d.Barked -= OnBarked; // <-- d.Barked is null at this point.
            //
            //if (sourceEvent == null)
            //{
            //    throw new ArgumentNullException("sourceHandler");
            //}

            if (strongEventHandler == null)
            {
                throw new ArgumentNullException("strongEventHandler");
            }

            // Look for the weak event handler in the invocation list
            IWeakEventHandler<TCustomArgs> wehForEh =
                           (sourceEvent==null ? new Delegate[0] : sourceEvent.GetInvocationList())
                           .Select(ExtractDelegateTarget)
                           .OfType<IWeakEventHandler<TCustomArgs>>()
                           .FirstOrDefault(weh => weh.IsHandlerFor(strongEventHandler));

            if (wehForEh!=null)
            {
                return wehForEh.HandlerT;
            }

            // return the strong event handler if we don't find a wrapped event handler
            return strongEventHandler;
        }

        private static bool IsAnonymousMethod(Delegate eventHandler)
        {
            return eventHandler.Method.DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0;
        }

        private static bool IsInstanceMethod(Delegate eventHandler)
        {
            return !eventHandler.Method.IsStatic && eventHandler.Target != null;
        }

        private static object ExtractDelegateTarget(Delegate source)
        {
            return source.Target;
        }

        // This is a performance optimisation. Since C# can't have static method variables, the
        // workaround is to declare a static class member variable.
        private static class WeakEventHandlerHelpersT<TCustomArgs>
            where TCustomArgs : EventArgs
        {
            // Is the method a weak event handler?
            public static bool IsWeakEventHandler(Delegate declaringType)
            {
                return WehType.IsAssignableFrom(declaringType.Method.DeclaringType);
            }
            
            // Performance - since this is a static generic class, there will be one instance per class generic argument. 
            // See http://stackoverflow.com/questions/3037203/are-static-members-of-a-generic-class-tied-to-the-specific-instance
            private static readonly Type WehType = typeof(IWeakEventHandler<>).MakeGenericType(typeof(TCustomArgs));
        }
    }
}
