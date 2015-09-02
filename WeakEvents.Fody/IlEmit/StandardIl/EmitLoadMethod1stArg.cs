using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // Loads the 1st method argument, accounting for the static modifier
    class EmitLoadMethod1stArg : IlEmitterBase
    {
        public EmitLoadMethod1stArg(IlEmitter preceedingCode)
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
        public static IlEmitter LoadMethod1stArg(this IlEmitter preceedingCode)
        {
            return new EmitLoadMethod1stArg(preceedingCode);
        }
    }
}
