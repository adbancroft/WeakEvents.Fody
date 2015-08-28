using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitLdToken : IlEmitterBase
    {
        private readonly TypeReference _targetType;

        public EmitLdToken(IlEmitter preceedingCode, TypeReference targetType)
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
        public static IlEmitter LdToken(this IlEmitter preceedingCode, TypeReference targetType)
        {
            return new EmitLdToken(preceedingCode, targetType);
        }
        public static IlEmitter LdToken(this MethodDefinition method, TypeReference targetType)
        {
            return LdToken(new EmptyEmitter(method), targetType);
        }
    }
}
