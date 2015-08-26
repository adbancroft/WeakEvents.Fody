using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using WeakEvents.Fody;

namespace WeakEvents.Fody.Test
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class TestTypeDefinitionExtensions
    {
        private ModuleDefinition _moduleDef;


        [TestInitialize]
        public void LoadTypes()
        {
            _moduleDef = ModuleDefinition.ReadModule(this.GetType().Assembly.Location, new ReaderParameters
            {
            });
        }

        [TestMethod]
        public void IsTypeToProcess_NoEvents_False()
        {
            Assert.IsFalse(TypeDefinitionExtensions.IsTypeToProcess(_moduleDef.Types.Single(t => t.FullName.Equals(typeof(NoEvents).FullName))));
        }

        [TestMethod]
        public void IsTypeToProcess_Interface_False()
        {
            Assert.IsFalse(TypeDefinitionExtensions.IsTypeToProcess(_moduleDef.Types.Single(t => t.FullName.Equals(typeof(IInterface).FullName))));
        }

        [TestMethod]
        public void IsTypeToProcess_NoAttribute_False()
        {
            Assert.IsFalse(TypeDefinitionExtensions.IsTypeToProcess(_moduleDef.Types.Single(t => t.FullName.Equals(typeof(EventButNoAttribute).FullName))));
        }

        [TestMethod]
        public void IsTypeToProcess_Valid_True()
        {
            Assert.IsTrue(TypeDefinitionExtensions.IsTypeToProcess(_moduleDef.Types.Single(t => t.FullName.Equals(typeof(EventWithAttribute).FullName))));
        }        
    }
}
