using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    // Concatenate emitters
    internal class EmitConcat : IlEmitterBase
    {
        readonly private ILEmitter _second;

        public EmitConcat(ILEmitter first, ILEmitter second)
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
        public static ILEmitter Concat(this ILEmitter first, ILEmitter second)
        {
            return new EmitConcat(first, second);
        }
    }
}