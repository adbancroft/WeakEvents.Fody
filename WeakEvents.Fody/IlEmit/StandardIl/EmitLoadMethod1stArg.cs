using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitLoadMethod1stArg : IlEmitterBase
    {
        public EmitLoadMethod1stArg(IlEmitter preceedingCode)
            : base(preceedingCode)
        {
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldarg_1) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter LoadMethod1stArg(this IlEmitter preceedingCode)
        {
            return new EmitLoadMethod1stArg(preceedingCode);
        }
        public static IlEmitter LoadMethod1stArg(this MethodDefinition method)
        {
            return LoadMethod1stArg(new EmptyEmitter(method));
        }
    }
}
