using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeakEvents.Fody;
using System.Reflection;
using System.IO;
using Mono.Cecil;

namespace WeaverMemoryTester
{
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

        public dynamic GetInstance(string className)
        {
            var type = Assembly.GetType(className, true);
            return Activator.CreateInstance(type);
        }
    }

    class EventTarget
    {
        public int FireCount;
        public void EventHandler(object o, EventArgs args)
        {
            ++FireCount;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var weaverHelper = new WeaverHelper("AssemblyToProcessDotNet4.dll");

            // Setup event source, target & wire together
            var target = new EventTarget();
            var wr = new WeakReference(target);

            //var source = weaverHelper.GetInstance("AssemblyToProcessDotNet4.AutoEventHandler");
            //var source = new ReferenceAssembly.Sample();
            var source = new AssemblyToProcessDotNet4.AutoEventHandler();
            source.WillBeWeak1 += new EventHandler(target.EventHandler);

            // Confirm event fires correctly.
            //Assert.IsNotNull(wr.Target);
            source.FireWillBeWeak1();
            //Assert.AreEqual(1, target.FireCount);
            Console.WriteLine("Pausing 5 seconds.");
            System.Threading.Thread.Sleep(5000);

            // Confirm that the event source does not keep the target alive.
            target = null;
            System.GC.Collect();
            //Assert.IsNull(wr.Target);

            Console.WriteLine("Pausing 5 seconds.");
            System.Threading.Thread.Sleep(5000);

            // Fire the event again to force clean up of the weak event handler instance
            source.FireWillBeWeak1();

            // Keep the source alive until here: if it's GC'd this test will be invalid.
            System.GC.KeepAlive(source);
        }
    }
}
