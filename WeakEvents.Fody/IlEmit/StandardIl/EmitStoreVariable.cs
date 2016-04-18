using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Stloc
    internal class EmitStoreVariable : IlEmitterBase
    {
        private readonly VariableDefinition _variableDef;
        private readonly ILEmitter _variableValueGenerator;

        public EmitStoreVariable(ILEmitter preceedingCode, VariableDefinition variableDef, ILEmitter variableValueGenerator)
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
        public static ILEmitter Store(this ILEmitter preceedingCode, VariableDefinition variableDef, ILEmitter variableValueGenerator)
        {
            return new EmitStoreVariable(preceedingCode, variableDef, variableValueGenerator);
        }
    }
}