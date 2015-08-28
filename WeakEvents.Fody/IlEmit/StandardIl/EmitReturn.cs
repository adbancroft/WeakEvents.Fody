using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitReturn : IlEmitterBase
    {
        public EmitReturn(IlEmitter preceedingCode)
            : base(preceedingCode)
        {
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ret) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Return(this IlEmitter preceedingCode)
        {
            return new EmitReturn(preceedingCode);
        }
        public static IlEmitter Return(this MethodDefinition method)
        {
            return Return(new EmptyEmitter(method));
        }
    }
}
