using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Castclass
    internal class EmitCastClass : IlEmitterBase
    {
        private readonly TypeReference _targetType;

        public EmitCastClass(ILEmitter preceedingCode, TypeReference targetType)
            : base(preceedingCode)
        {
            _targetType = targetType;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Castclass, _targetType) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter CastClass(this ILEmitter preceedingCode, TypeReference targetType)
        {
            return new EmitCastClass(preceedingCode, targetType);
        }
    }
}