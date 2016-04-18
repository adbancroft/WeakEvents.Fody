using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // Loads the 1st method argument, accounting for the static modifier
    internal class EmitLoadMethodFirstArg : IlEmitterBase
    {
        public EmitLoadMethodFirstArg(ILEmitter preceedingCode)
            : base(preceedingCode)
        {
        }

        public override IEnumerable<Instruction> Emit()
        {
            if (Method.IsStatic)
            {
                return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldarg_0) });
            }

            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldarg_1) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter LoadMethodFirstArg(this ILEmitter preceedingCode)
        {
            return new EmitLoadMethodFirstArg(preceedingCode);
        }
    }
}