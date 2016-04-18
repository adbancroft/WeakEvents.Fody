using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.Other
{
    // Emit IL for a call to Delegate.Remove(itemToRemoveFrom, itemToRemove)
    internal class EmitDelegateRemove : IlEmitterBase
    {
        private readonly ILEmitter _inner;

        public EmitDelegateRemove(ILEmitter preceedingCode, ILEmitter itemToRemoveFrom, ILEmitter itemToRemove)
            : base(preceedingCode)
        {
            _inner = new EmptyEmitter(preceedingCode).Call(Importer.DelegateRemove, itemToRemoveFrom, itemToRemove);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter CallDelegateRemove(this ILEmitter preceedingCode, ILEmitter itemToRemoveFrom, ILEmitter itemToRemove)
        {
            return new EmitDelegateRemove(preceedingCode, itemToRemoveFrom, itemToRemove);
        }
    }
}