using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    // IL is a stack based programming language based on low level primitives.
    // This interface, its implementations and extension methods attempt to
    // make IL emitting safer (by enforcing some type safety) and easier
    // (by taking care of ordering IL and making code more readable).
    //
    // They emitters are designed to be chained together, ala LINQ.
    internal interface IlEmitter
    {
        // Call this to generate the instructions
        IEnumerable<Instruction> Emit();

        // The method the instructions are for.
        MethodDefinition Method { get; }

        // A class to import items into the module
        ModuleImporter Importer { get; }
    }

    // Base emitter that encapsulates common concepts
    internal abstract class IlEmitterBase : IlEmitter
    {
        private IlEmitter _preceedingCode;

        // Code is linear: each emitter must have a preceeding emitter
        protected IlEmitterBase(IlEmitter preceedingCode)
        {
            _preceedingCode = preceedingCode;
        }

        public abstract IEnumerable<Instruction> Emit();

        // Called by derived classes as part of their Emit() method.
        protected IEnumerable<Instruction> EmitPreceeding()
        {
            return _preceedingCode.Emit();
        }

        // Chain the method definition.
        public MethodDefinition Method { get { return _preceedingCode.Method; } }

        // Chain the importer.
        public ModuleImporter Importer { get { return _preceedingCode.Importer; } }
    }
}