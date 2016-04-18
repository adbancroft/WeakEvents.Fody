using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Ldtoken
    internal class EmitLdToken : IlEmitterBase
    {
        private readonly TypeReference _targetType;

        public EmitLdToken(ILEmitter preceedingCode, TypeReference targetType)
            : base(preceedingCode)
        {
            _targetType = targetType;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldtoken, _targetType) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter LdToken(this ILEmitter preceedingCode, TypeReference targetType)
        {
            return new EmitLdToken(preceedingCode, targetType);
        }
    }
}