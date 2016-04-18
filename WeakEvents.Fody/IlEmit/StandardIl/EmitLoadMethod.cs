using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Ldftn
    internal class EmitLoadMethod : IlEmitterBase
    {
        private readonly MethodReference _targetMethod;

        public EmitLoadMethod(ILEmitter preceedingCode, MethodReference targetMethod)
            : base(preceedingCode)
        {
            _targetMethod = targetMethod;
        }

        public override IEnumerable<Instruction> Emit()
        {
            if (Method.IsStatic)
            {
                return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldnull), Instruction.Create(OpCodes.Ldftn, _targetMethod) });
            }

            return EmitPreceeding().Concat(new[] { Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldftn, _targetMethod) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter LoadMethod(this ILEmitter preceedingCode, MethodReference targetMethod)
        {
            return new EmitLoadMethod(preceedingCode, targetMethod);
        }
    }
}