using System.Linq;
using Mono.Cecil;

namespace WeakEvents.Fody
{
    public static class TypeDefinitionExtensions
    {
        public static bool IsTypeToProcess(this TypeDefinition typeDefinition)
        {
            return typeDefinition.HasEvents && !typeDefinition.IsInterface && HasWeakEventAttribute(typeDefinition);
        }

        private static bool HasWeakEventAttribute(TypeDefinition typeDefinition)
        {
            return typeDefinition.CustomAttributes.Any(attr => typeof(ImplementWeakEventsAttribute).FullName.Equals(attr.AttributeType.FullName));
        }
    }
}