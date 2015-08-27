using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    class EmitConcat : IlEmitterBase
    {
        private IlEmitter _first;
        private IlEmitter _second;

        public EmitConcat(IlEmitter preceedingCode, IlEmitter first, IlEmitter second)
            : base(preceedingCode)
        {
            _first = first;
            _second = second;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_first.Emit()).Concat(_second.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Concat(this IlEmitter preceedingCode, IlEmitter first, IlEmitter second)
        {
            return new EmitConcat(preceedingCode, first, second);
        }
        public static IlEmitter Concat(this MethodDefinition method, IlEmitter first, IlEmitter second)
        {
            return Concat(new EmptyEmitter(method), first, second);
        }
    }
}
