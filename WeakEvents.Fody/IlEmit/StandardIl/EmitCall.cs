using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Call
    class EmitCall : IlEmitterBase
    {
        private readonly IlEmitter _methodParameters;
        private readonly MethodReference _targetMethod;

        public EmitCall(IlEmitter preceedingCode, MethodReference targetMethod, params IlEmitter[] methodParameters)
            : base(preceedingCode)
        {
            _methodParameters = methodParameters.Aggregate((prev, next) => prev.Concat(next));
            _targetMethod = targetMethod;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_methodParameters.Emit())
                                   .Concat(new [] { Instruction.Create(OpCodes.Call, _targetMethod) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Call(this IlEmitter preceedingCode, MethodReference targetMethod, params IlEmitter[] methodParameters)
        {
            return new EmitCall(preceedingCode, targetMethod, methodParameters);
        }
        public static IlEmitter Call(this MethodDefinition method, MethodReference targetMethod, params IlEmitter[] methodParameters)
        {
            return Call(new EmptyEmitter(method), targetMethod, methodParameters);
        }
    }
}
