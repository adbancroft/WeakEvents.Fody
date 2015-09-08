using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Newobj
    internal class EmitNewObject : IlEmitterBase
    {
        private readonly IlEmitter _ctorParameters;
        private readonly MethodReference _ctor;

        public EmitNewObject(IlEmitter preceedingCode, MethodReference ctor, params IlEmitter[] ctorParameters)
            : base(preceedingCode)
        {
            _ctorParameters = ctorParameters.Aggregate((prev, next) => prev.Concat(next));
            _ctor = ctor;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_ctorParameters.Emit())
                                   .Concat(new[] { Instruction.Create(OpCodes.Newobj, _ctor) });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter NewObject(this IlEmitter preceedingCode, MethodReference ctor, params IlEmitter[] ctorParameters)
        {
            return new EmitNewObject(preceedingCode, ctor, ctorParameters);
        }
    }
}