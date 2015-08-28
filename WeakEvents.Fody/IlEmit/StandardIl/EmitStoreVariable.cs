using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitStoreVariable : IlEmitterBase
    {
        private readonly VariableDefinition _variableDef;
        private readonly IlEmitter _variableValueGenerator;

        public EmitStoreVariable(IlEmitter preceedingCode, VariableDefinition variableDef, IlEmitter variableValueGenerator)
            : base(preceedingCode)
        {
            _variableDef = variableDef;
            _variableValueGenerator = variableValueGenerator;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_variableValueGenerator.Emit())
                                   .Concat(new[] { Instruction.Create(OpCodes.Stloc, _variableDef) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Store(this IlEmitter preceedingCode, VariableDefinition variableDef, IlEmitter variableValueGenerator)
        {
            return new EmitStoreVariable(preceedingCode, variableDef, variableValueGenerator);
        }
        public static IlEmitter Store(this MethodDefinition method, VariableDefinition variableDef, IlEmitter variableValueGenerator)
        {
            return Store(new EmptyEmitter(method), variableDef, variableValueGenerator);
        }
    }
}
