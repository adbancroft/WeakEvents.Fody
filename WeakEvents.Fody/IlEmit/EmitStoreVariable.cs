using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    class EmitStoreVariable : IlEmitterBase
    {
        private VariableDefinition _variableDef;

        public EmitStoreVariable(IlEmitter preceedingCode, VariableDefinition variableDef)
            : base(preceedingCode)
        {
            _variableDef = variableDef;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Stloc, _variableDef) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Store(this IlEmitter preceedingCode, VariableDefinition variableDef)
        {
            return new EmitStoreVariable(preceedingCode, variableDef);
        }
        public static IlEmitter Store(this MethodDefinition method, VariableDefinition variableDef)
        {
            return Store(new EmptyEmitter(method), variableDef);
        }
    }
}
