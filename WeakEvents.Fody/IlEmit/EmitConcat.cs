using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    class EmitConcat : IlEmitterBase
    {
        private IlEmitter _second;

        public EmitConcat(IlEmitter first, IlEmitter second)
            : base(first)
        {
            _second = second;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_second.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter Concat(this IlEmitter first, IlEmitter second)
        {
            return new EmitConcat(first, second);
        }
    }
}
