﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace WeakEvents.Runtime
{
    /// <summary>
    /// A factory for IWeakEventHandler&lt;TCustomArgs&gt; instances.
    /// </summary>
    /// <typeparam name="TCustomArgs">The event argument class, derived from <see cref="EventArgs"/></typeparam>
    internal static class WeakEventHandlerFactory<TCustomArgs>
        where TCustomArgs : EventArgs
    {
        /// <summary>
        /// Create a IWeakEventHandler&lt;TCustomArgs&gt; instance for the stong event handler
        /// </summary>
        /// <param name="eventHandler">The strong event handler</param>
        /// <param name="unsubscribe">An action which the weak event handler can use to unsubscribe from the source event</param>
        public static IWeakEventHandler<TCustomArgs> Create(EventHandler<TCustomArgs> eventHandler, Action<EventHandler<TCustomArgs>> unsubscribe)
        {
            return (IWeakEventHandler<TCustomArgs>)GetConstructorInfo(eventHandler.GetMethodInfo().DeclaringType)
                                .Invoke(new object[] { eventHandler, unsubscribe });
        }

        // Get the ConstructorInfo for a WeakEventHandler<,> type that could support the event handler.
        private static ConstructorInfo GetConstructorInfo(Type eventHandlerDeclaringType)
        {
            ConstructorInfo ci;
            if (!_constructorInfoCache.TryGetValue(eventHandlerDeclaringType, out ci))
            {
                ci = CreateConstructorInfo(eventHandlerDeclaringType);
                _constructorInfoCache[eventHandlerDeclaringType] = ci;
            }
            return ci;
        }

        // Use reflection to provide a class constructor for a class that implements WeakEventHandler<,>
        private static ConstructorInfo CreateConstructorInfo(Type eventHandlerDeclaringType)
        {
            Type wehType = typeof(WeakEventHandler<,>).MakeGenericType(eventHandlerDeclaringType, typeof(TCustomArgs));
            return wehType.GetTypeInfo().DeclaredConstructors.First(ParametersMatch);
        }

        private static bool ParametersMatch(MethodBase mi)
        {
            var methodParams = mi.GetParameters();
            return methodParams.Length == 2
                && methodParams[0].ParameterType.Equals(typeof(EventHandler<TCustomArgs>))
                && methodParams[1].ParameterType.Equals(typeof(Action<EventHandler<TCustomArgs>>));
        }

        // The ConstructorInfo cache.
        // Since this is a static generic class, there will be one instance per class generic argument. So no need
        // to key on TCustomArgs.
        // See http://stackoverflow.com/questions/3037203/are-static-members-of-a-generic-class-tied-to-the-specific-instance
#pragma warning disable S2743 // Static fields should not be used in generic types
        private static readonly IDictionary<Type, ConstructorInfo> _constructorInfoCache = new Dictionary<Type, ConstructorInfo>();
#pragma warning restore S2743 // Static fields should not be used in generic types
    }
}