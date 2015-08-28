using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.Other
{
    class EmitDelegateRemove : IlEmitterBase
    {
        private IlEmitter _inner;

        public EmitDelegateRemove(IlEmitter preceedingCode, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
            : base(preceedingCode)
        {
            _inner = Method.Call(LoadDelegateRemoveMethodDefinition(), itemToRemoveFrom.Concat(itemToRemove));
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }

        private MethodReference LoadDelegateRemoveMethodDefinition()
        {
            var moduleDef = Method.Module;
            var delegateReference = moduleDef.Import(typeof(System.Delegate));
            var delegateDefinition = delegateReference.Resolve();
            var methodDefinition = delegateDefinition.Methods
                .Single(x =>
                    x.Name == "Remove" &&
                    x.Parameters.Count == 2 &&
                    x.Parameters.All(p => p.ParameterType == delegateDefinition));
            return moduleDef.Import(methodDefinition);
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter CallDelegateRemove(this IlEmitter preceedingCode, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
        {
            return new EmitDelegateRemove(preceedingCode, itemToRemoveFrom, itemToRemove);
        }
        public static IlEmitter CallDelegateRemove(this MethodDefinition method, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
        {
            return CallDelegateRemove(new EmptyEmitter(method), itemToRemoveFrom, itemToRemove);
        }
    }
}
