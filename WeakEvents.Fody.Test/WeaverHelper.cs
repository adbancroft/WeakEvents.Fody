using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;

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
            var assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            BeforeAssemblyPath = Path.Combine(assemblyFolder, assemblyToProcess);
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

            //ProvideIlStepThrough(AfterAssemblyPath);

            Assembly = Assembly.LoadFile(AfterAssemblyPath);
        }

        private static void ProvideIlStepThrough(string assemblyPath)
        {
            // In order to step through IL....
            // Decompile
            var ilFile = Path.ChangeExtension(assemblyPath, "il");
            var ildasm = System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\ildasm.exe", "/out=" + ilFile + " " + assemblyPath);
            ildasm.WaitForExit();
            // Recompile to new binary + symbols.
            var ilasm = System.Diagnostics.Process.Start(@"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe", ilFile + " /dll /debug /output=" + assemblyPath);
            ilasm.WaitForExit();
        }

        public object GetInstance(string className)
        {
            var type = Assembly.GetType(className, true);
            return Activator.CreateInstance(type);
        }
    }
}