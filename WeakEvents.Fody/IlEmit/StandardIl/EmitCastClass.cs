using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitCastClass : IlEmitterBase
    {
        private TypeReference _targetType;

        public EmitCastClass(IlEmitter preceedingCode, TypeReference targetType)
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
        public static IlEmitter CastClass(this IlEmitter preceedingCode, TypeReference targetType)
        {
            return new EmitCastClass(preceedingCode, targetType);
        }
        public static IlEmitter CastClass(this MethodDefinition method, TypeReference targetType)
        {
            return CastClass(new EmptyEmitter(method), targetType);
        }
    }
}
