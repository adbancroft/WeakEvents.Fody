using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.Other
{
    // Emit IL for a call to Delegate.Remove(itemToRemoveFrom, itemToRemove)
    internal class EmitDelegateRemove : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitDelegateRemove(IlEmitter preceedingCode, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
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
        public static IlEmitter CallDelegateRemove(this IlEmitter preceedingCode, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
        {
            return new EmitDelegateRemove(preceedingCode, itemToRemoveFrom, itemToRemove);
        }
    }
}