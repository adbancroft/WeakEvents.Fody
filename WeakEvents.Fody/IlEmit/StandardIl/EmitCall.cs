using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitCall : IlEmitterBase
    {
        private IlEmitter _methodParameters;
        private MethodReference _targetMethod;

        public EmitCall(IlEmitter preceedingCode, MethodReference targetMethod, IlEmitter methodParameters)
            : base(preceedingCode)
        {
            _methodParameters = methodParameters;
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
        public static IlEmitter Call(this IlEmitter preceedingCode, MethodReference targetMethod, IlEmitter methodParameters)
        {
            return new EmitCall(preceedingCode, targetMethod, methodParameters);
        }
        public static IlEmitter Call(this MethodDefinition method, MethodReference targetMethod, IlEmitter methodParameters)
        {
            return Call(new EmptyEmitter(method), targetMethod, methodParameters);
        }
    }
}
