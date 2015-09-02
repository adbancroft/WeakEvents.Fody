using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.Other;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.DelegateConvert(itemToConvert, targetType)
    class EmitDelegateConvert : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitDelegateConvert(IlEmitter preceedingCode, IlEmitter itemToConvert, TypeReference targetType)
            : base(preceedingCode)
        {
            _inner = new EmptyEmitter(preceedingCode)
                        .Call(Importer.DelegateConvertChangeType, itemToConvert, new EmptyEmitter(preceedingCode).TypeOf(targetType))
                        .CastClass(targetType);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter DelegateConvert(this IlEmitter preceedingCode, IlEmitter itemToConvert, TypeReference targetType)
        {
            return new EmitDelegateConvert(preceedingCode, itemToConvert, targetType);
        }
    }
}
