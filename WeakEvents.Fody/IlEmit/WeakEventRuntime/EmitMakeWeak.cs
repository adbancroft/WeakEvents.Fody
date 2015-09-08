using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.MakeWeak<T>()
    internal class EmitMakeWeak : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitMakeWeak(IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandler, IlEmitter unsubscribe)
            : base(preceedingCode)
        {
            var openMakeWeak = Importer.OpenMakeWeakT;
            _inner = new EmptyEmitter(preceedingCode).Call(openMakeWeak.MakeMethodClosedGeneric(closedHandlerType.GenericArguments[0]), eventHandler, unsubscribe);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter MakeWeak(this IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandler, IlEmitter unsubscribe)
        {
            return new EmitMakeWeak(preceedingCode, closedHandlerType, eventHandler, unsubscribe);
        }
    }
}