using System;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace WeakEvents.Fody
{
    public class ModuleWeaver : ILogger
    {
        // Will log an MessageImportance.Normal message to MSBuild.
        public Action<string> LogDebug { get; set; }

        // Will log an MessageImportance.High message to MSBuild.
        public Action<string> LogInfo { get; set; }

        // Will log an warning message to MSBuild.
        public Action<string> LogWarning { get; set; }

        // Will log an error message to MSBuild.
        public Action<string> LogError { get; set; }

        // An instance of Mono.Cecil.ModuleDefinition for processing
        [CLSCompliant(false)]
        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            // Init logging delegates to make testing easier
            LogDebug = m => { };
            LogInfo = m => { };
            LogWarning = m => { };
            LogError = m => { };
        }

        // Fody works by convention & will call this method to run the weaver.
        public void Execute()
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