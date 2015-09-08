using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Ret
    internal class EmitReturn : IlEmitterBase
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
    }
}