using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using WeakEvents.Fody;

namespace WeakEvents.Fody.Test
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class WeaverHelper
    {
        public string BeforeAssemblyPath { get; private set; }
        public string AfterAssemblyPath { get; private set; }
        public Assembly Assembly { get; private set; }

        public WeaverHelper(string assemblyToProcess)
        {
            BeforeAssemblyPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), assemblyToProcess);
            AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");

            File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
            var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath, new ReaderParameters());
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition,
                LogDebug = s => System.Diagnostics.Debug.WriteLine(s, "Debug"),
                LogInfo = s => System.Diagnostics.Debug.WriteLine(s, "Info"),
                LogWarning = s => System.Diagnostics.Debug.WriteLine(s, "Warning"),
                LogError = s => System.Diagnostics.Debug.WriteLine(s, "Error"),
            };

            weavingTask.Execute();

            moduleDefinition.Write(AfterAssemblyPath);

            Assembly = Assembly.LoadFile(AfterAssemblyPath);
        }

        public object GetInstance(string className)
        {
            var type = Assembly.GetType(className, true);
            return Activator.CreateInstance(type);
        }
    }
}