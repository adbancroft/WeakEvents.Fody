using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace WeakEvents.Fody.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TestDelegateConvertWeaver
    {
        private DelegateConvertWeaver _weaver;
        private ModuleDefinition _moduleDef;

        [TestInitialize]
        public void LoadTypes()
        {
            _moduleDef = ModuleDefinition.ReadModule("AssemblyToProcessDotNet4.dll", new ReaderParameters());
            _weaver = new DelegateConvertWeaver(_moduleDef);
        }

        [TestMethod]
        public void AddChangeTypeCall_Converts()
        {
            // Grab the method to weave
            var delegateConverter = _moduleDef.Types.Single(t => t.FullName.Equals(typeof(AssemblyToProcessDotNet4.DelegateConverter).FullName));
            var convert = delegateConverter.Methods.Single(m => m.Name.Equals("Convert"));

            // Weave the method call
            var instructions = convert.Body.Instructions;
            instructions.Clear();
            instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            // This is the method under test
            _weaver.AddChangeTypeCall(instructions, convert.ReturnType);
            instructions.Add(Instruction.Create(OpCodes.Ret));

            // Load an instance of the woven type.
            var converter = (TestInterfaces.IDelegateConverter)CreateInstance(_moduleDef, typeof(AssemblyToProcessDotNet4.DelegateConverter));

            // Run the woven code
            var result = converter.Convert((object sender, AssemblyLoadEventArgs args) => { });
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(EventHandler<AssemblyLoadEventArgs>));
        }

        [TestMethod]
        public void InsertChangeTypeCall_Converts()
        {
            // Grab the method to weave
            var delegateConverter = _moduleDef.Types.Single(t => t.FullName.Equals(typeof(AssemblyToProcessDotNet4.DelegateConverter).FullName));
            var convert = delegateConverter.Methods.Single(m => m.Name.Equals("Convert"));

            // Weave the method call
            var instructions = convert.Body.Instructions;
            instructions.Clear();
            instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            instructions.Add(Instruction.Create(OpCodes.Ret));
            // This is the method under test
            _weaver.InsertChangeTypeCall(instructions, 1, convert.ReturnType);

            // Load an instance of the woven type.
            var converter = (TestInterfaces.IDelegateConverter)CreateInstance(_moduleDef, typeof(AssemblyToProcessDotNet4.DelegateConverter));

            // Run the woven code
            var result = converter.Convert((object sender, AssemblyLoadEventArgs args) => { });
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(EventHandler<AssemblyLoadEventArgs>));
        }

        private static object CreateInstance(ModuleDefinition moduleDef, Type toLoad)
        {
            var assembly = Load(moduleDef);
            return assembly.CreateInstance(toLoad.FullName);
        }

        private static Assembly Load(ModuleDefinition moduleDef)
        {
            var mem = new MemoryStream();
            moduleDef.Write(mem);
            mem.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(mem.GetBuffer());
        }
    }
}
