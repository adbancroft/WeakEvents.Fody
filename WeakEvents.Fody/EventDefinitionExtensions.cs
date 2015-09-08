using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody
{
    public static class EventDefinitionExtensions
    {
        #region GetEventDelegate

        /// <summary>
        /// Finds the delegate field that backs an event.
        /// </summary>
        /// <param name="eventt">The event to find the delegate for</param>
        /// <returns>The field; null if we could not determine the field</returns>
        public static FieldReference GetEventDelegate(this EventDefinition eventt)
        {
            // There is no direct link from an event to it's backing delegate field - in fact, there doesn't have to be a backing delegate.
            //
            // So we have to use heuristics.
            var candidateFields = eventt.DeclaringType.Fields.Where(f =>
            {
                // Find all fields:
                // 1. With the same type as the event.
                return f.FieldType.FullName.Equals(eventt.EventType.FullName)
                    // 2. Used by the add method + same static modifier
                    && eventt.AddMethod.IsStatic == f.IsStatic
                    && IsUsedByMethod(eventt.AddMethod, f)
                    // 3. Used by the remove method + same static modifier
                    && eventt.RemoveMethod.IsStatic == f.IsStatic
                    && IsUsedByMethod(eventt.RemoveMethod, f);
            }).ToArray();

            // If we end up with a single field, we assume that is the backing delegate
            if (candidateFields.Length == 1)
            {
                return candidateFields[0];
            }

            return null;
        }

        private static bool IsUsedByMethod(MethodDefinition method, FieldReference field)
        {
            return method.Body.Instructions.Any(i => IsLoadField(i, field));
        }

        private static bool IsLoadField(Instruction i, FieldReference field)
        {
            return (i.OpCode.Code == Code.Ldfld || i.OpCode.Code == Code.Ldsfld) && i.Operand.Equals(field);
        }

        #endregion GetEventDelegate

        #region GetEventArgsType

        public static TypeReference GetEventArgsType(this TypeReference typeRef)
        {
            return GetGenericEventArgsType(typeRef) ?? GetNonGenericEventArgsType(typeRef);
        }

        private static TypeReference GetNonGenericEventArgsType(TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();
            var invokeMethod = typeDef.Methods.Single(m => m.Name.Equals("Invoke"));
            return invokeMethod.Parameters[1].ParameterType;
        }

        private static TypeReference GetGenericEventArgsType(TypeReference typeRef)
        {
            var genericInstance = typeRef as GenericInstanceType;
            return genericInstance == null ? null : genericInstance.GenericArguments[0];
        }

        #endregion GetEventArgsType

        #region IsValidEventDelegate

        public static bool IsValidEventDelegate(this TypeReference typeRef)
        {
            return IsGenericEventHandler(typeRef) || IsValidNonGenericEventDelegate(typeRef);
        }

        // Is this EventHandler<T>?
        private static bool IsGenericEventHandler(TypeReference typeRef)
        {
            var genericInstance = typeRef as GenericInstanceType;
            return genericInstance != null
                 && genericInstance.ElementType.FullName.Equals(EventHandlerTName)
                 && genericInstance.HasGenericArguments
                 && IsEventArgs(genericInstance.GenericArguments[0]);
        }

        // We also support event handlers that semantically match EventHandler<T>
        // i.e. the event delegate has 2 parameters: 1st is object, 2nd is EventArgs or a derivative
        private static bool IsValidNonGenericEventDelegate(this TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();

            return IsMulticastDelegate(typeDef) && HasStandardInvokeMethod(typeDef);
        }

        private static bool HasStandardInvokeMethod(TypeDefinition typeDef)
        {
            var invokeMethod = typeDef.Methods.Single(m => m.Name.Equals("Invoke"));

            return invokeMethod.Parameters.Count == 2
                && invokeMethod.Parameters[0].ParameterType.FullName.Equals(SysObjectName)
                && IsEventArgs(invokeMethod.Parameters[1].ParameterType);
        }

        private static bool IsEventArgs(TypeReference typeReference)
        {
            return typeReference.FullName.Equals(EventArgsName)
                || IsDerivedFromEventArgs(typeReference);
        }

        private static bool IsDerivedFromEventArgs(TypeReference typeReference)
        {
            var typeDef = typeReference.Resolve();
            return typeDef.BaseType != null
                && IsEventArgs(typeDef.BaseType);
        }

        private static bool IsMulticastDelegate(TypeDefinition type)
        {
            return type.BaseType.FullName.Equals(MulticastDelegateName);
        }

        #endregion IsValidEventDelegate

        private static string SysObjectName = typeof(System.Object).FullName;
        private static string MulticastDelegateName = typeof(System.MulticastDelegate).FullName;
        private static string EventArgsName = typeof(System.EventArgs).FullName;
        private static string EventHandlerTName = typeof(System.EventHandler<>).FullName;
    }
}