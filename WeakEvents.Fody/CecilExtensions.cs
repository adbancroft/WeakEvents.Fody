using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace WeakEvents.Fody
{
    internal static class CecilExtensions
    {
        // Takes a method from an *open* generic type and creates a method on a *closed* generic type
        // E.g. Action<T>.ctor(Object, IntPtr) to Action<EventArgs>.ctor(Object, IntPtr)
        public static MethodReference MakeDeclaringTypeClosedGeneric(this MethodReference self, params TypeReference[] genericArguments)
        {
            var reference = new MethodReference(
                self.Name,
                self.ReturnType,
                self.DeclaringType.MakeGenericInstanceType(genericArguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };
            Append(reference.Parameters, self.Parameters);
            Append(reference.GenericParameters, self.GenericParameters);

            return reference;
        }

        // Takes an *open* generic method and creates a *closed* generic method
        // E.g. void Foo<T>() to void Foo<int>().
        public static MethodReference MakeMethodClosedGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException("Argument count mismatch", "self");

            var instance = new GenericInstanceMethod(self);
            Append(instance.GenericArguments, arguments);

            return instance;
        }

        private static void Append<T>(ICollection<T> collection, IEnumerable<T> newItems)
        {
            foreach (var item in newItems)
                collection.Add(item);
        }
    }
}