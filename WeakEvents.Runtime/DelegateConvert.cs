using System;

namespace WeakEvents.Runtime
{
    /// <summary>
    /// A helper class to convert between delegate types, modeled after System.Convert.
    ///
    /// Delegates with exactly the same parameters and return type are not compatible.
    /// Specifically, you cannot cast a delegate to a delegate of another type even if
    /// they have the same parameters and return type. Use this class instead.
    ///
    /// Inspired by http://code.logos.com/blog/2008/07/casting_delegates.html
    /// </summary>
    public static class DelegateConvert
    {
        /// <summary>
        /// Convert the source delegate to a different delegate type
        /// </summary>
        /// <example>
        /// <code>
        /// DelegateConvert.ChangeType&lt;EventHandler&lt;PropertyChangedEventArgs&gt;&gt;(new PropertyChangedEventHandler(...))
        /// </code>
        /// </example>
        /// <typeparam name="T">Type of delegate to convert to</typeparam>
        /// <param name="source">Delegate to convert</param>
        /// <returns>A new delegate of the appropriate type</returns>
        /// <exception cref="System.InvalidCastException">Thrown when the source delegate cannot be converted to the target delegate type</exception>
        public static T ChangeType<T>(Delegate source) where T : class
        {
            return ChangeType(source, typeof(T)) as T;
        }

        /// <summary>
        /// Convert the source delegate to a different delegate type
        /// </summary>
        /// <param name="source">Delegate to convert</param>
        /// <param name="type">Type of delegate to convert to</param>
        /// <returns>A new delegate of the appropriate type</returns>
        /// <exception cref="System.InvalidCastException">Thrown when the source delegate cannot be converted to the target delegate type</exception>
        public static Delegate ChangeType(Delegate source, Type type)
        {
            if (source == null)
            {
                return null;
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            // Types are already compatible, no conversion required.
            if (type.IsInstanceOfType(source))
            {
                return source;
            }

            try
            {
                // Unwrap each delegate, create a delegate of the appropriate type and combine them all.
                Delegate final = null;
                foreach (Delegate d in source.GetInvocationList())
                {
                    final = Delegate.Combine(final, Delegate.CreateDelegate(type, d.Target, d.Method));
                }

                return final;
            }
            catch (ArgumentException e)
            {
                // Uh oh, incompatible delegate types.
                throw new InvalidCastException("Failed to convert delegate", e);
            }
        }
    }
}