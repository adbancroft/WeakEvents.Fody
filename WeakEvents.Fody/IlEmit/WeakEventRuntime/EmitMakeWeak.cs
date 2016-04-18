using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.MakeWeak<T>()
    internal class EmitMakeWeak : IlEmitterBase
    {
        private readonly ILEmitter _inner;

        public EmitMakeWeak(ILEmitter preceedingCode, GenericInstanceType closedHandlerType, ILEmitter eventHandler, ILEmitter unsubscribe)
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
        public static ILEmitter MakeWeak(this ILEmitter preceedingCode, GenericInstanceType closedHandlerType, ILEmitter eventHandler, ILEmitter unsubscribe)
        {
            return new EmitMakeWeak(preceedingCode, closedHandlerType, eventHandler, unsubscribe);
        }
    }
}