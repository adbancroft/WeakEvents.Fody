using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    interface IlEmitter
    {
        IEnumerable<Instruction> Emit();
        MethodDefinition Method { get; }
    }

    abstract class IlEmitterBase  : IlEmitter
    {
        IlEmitter _preceedingCode;

        protected IlEmitterBase(IlEmitter preceedingCode)
        {
            _preceedingCode = preceedingCode; 
        }

        public abstract IEnumerable<Instruction> Emit();

        protected IEnumerable<Instruction> EmitPreceeding()
        {
            return _preceedingCode.Emit();
        }

        public MethodDefinition Method { get { return _preceedingCode.Method; } }
    }
}
