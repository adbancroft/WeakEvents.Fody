using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Linq;

using global::Fody;

namespace WeakEvents.Fody
{
    using System.Collections.Generic;

    public class ModuleWeaver : BaseModuleWeaver, ILogger
    {
        // Fody works by convention & will call this method to run the weaver.
        public override void Execute()
        {
            LogDebug("Beginning weak event weaving");

            var weakEventWeaver = new EventWeaver(ModuleDefinition, this);

            foreach (var typeDef in ModuleDefinition.Types)
            {
                if (typeDef.IsTypeToProcess())
                {
                    ProcessType(typeDef, weakEventWeaver);
                }
                else
                {
                    LogInfo("Skipping type " + typeDef);
                }
                RemoveAttributes(typeDef.CustomAttributes);
            }

            CleanReferences();

            LogDebug("Finished weak event weaving");
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        private void CleanReferences()
        {
            var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name.Equals(typeof(ImplementWeakEventsAttribute).Assembly.GetName().Name));
            if (referenceToRemove != null)
            {
                ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
            }
        }

        private void ProcessType(TypeDefinition typeToWeave, EventWeaver weakEventWeaver)
        {
            LogDebug("Beginning weak event weaving for " + typeToWeave.FullName);

            foreach (var eventt in typeToWeave.Events)
            {
                weakEventWeaver.ProcessEvent(eventt);
            }

            LogDebug("Finished weak event weaving for " + typeToWeave.FullName);
        }

        private void RemoveAttributes(Collection<CustomAttribute> customAttributes)
        {
            foreach (var customAttribute in customAttributes.Where(IsWeakEventAttribute).ToList())
            {
                customAttributes.Remove(customAttribute);
            }
        }

        private bool IsWeakEventAttribute(CustomAttribute attribute)
        {
            return attribute.Constructor.DeclaringType.FullName.Equals(typeof(ImplementWeakEventsAttribute).FullName);
        }
    }
}