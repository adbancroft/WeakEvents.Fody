using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.FindWeak<T>(eventHandlerDelegate, strongEventHandler)
    internal class EmitFindWeak : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitFindWeak(IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandlerDelegate, IlEmitter strongEventHandler)
            : base(preceedingCode)
        {
            var openFindWeak = Importer.OpenFindWeakT;
            _inner = new EmptyEmitter(preceedingCode).Call(openFindWeak.MakeMethodClosedGeneric(closedHandlerType.GenericArguments[0]), eventHandlerDelegate, strongEventHandler);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter FindWeak(this IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandlerDelegate, IlEmitter strongEventHandler)
        {
            return new EmitFindWeak(preceedingCode, closedHandlerType, eventHandlerDelegate, strongEventHandler);
        }
    }
}