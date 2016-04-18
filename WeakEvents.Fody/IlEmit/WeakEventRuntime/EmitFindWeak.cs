using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.FindWeak<T>(eventHandlerDelegate, strongEventHandler)
    internal class EmitFindWeak : IlEmitterBase
    {
        private readonly ILEmitter _inner;

        public EmitFindWeak(ILEmitter preceedingCode, GenericInstanceType closedHandlerType, ILEmitter eventHandlerDelegate, ILEmitter strongEventHandler)
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
        public static ILEmitter FindWeak(this ILEmitter preceedingCode, GenericInstanceType closedHandlerType, ILEmitter eventHandlerDelegate, ILEmitter strongEventHandler)
        {
            return new EmitFindWeak(preceedingCode, closedHandlerType, eventHandlerDelegate, strongEventHandler);
        }
    }
}