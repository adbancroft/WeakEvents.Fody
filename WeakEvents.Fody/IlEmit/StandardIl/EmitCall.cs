using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Call
    internal class EmitCall : IlEmitterBase
    {
        private readonly ILEmitter _methodParameters;
        private readonly MethodReference _targetMethod;

        public EmitCall(ILEmitter preceedingCode, MethodReference targetMethod, params ILEmitter[] methodParameters)
            : base(preceedingCode)
        {
            _methodParameters = methodParameters.Aggregate((prev, next) => prev.Concat(next));
            _targetMethod = targetMethod;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_methodParameters.Emit())
                                   .Concat(new[] { Instruction.Create(OpCodes.Call, _targetMethod) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter Call(this ILEmitter preceedingCode, MethodReference targetMethod, params ILEmitter[] methodParameters)
        {
            return new EmitCall(preceedingCode, targetMethod, methodParameters);
        }
    }
}