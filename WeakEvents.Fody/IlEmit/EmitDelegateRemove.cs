using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    class EmitDelegateRemove : IlEmitterBase
    {
        // Delegate.Remove()
        private MethodReference _delegateRemoveMethodRef;
        private IlEmitter _itemToRemoveFrom;
        private IlEmitter _itemToRemove;

        public EmitDelegateRemove(IlEmitter preceedingCode, IlEmitter itemToRemoveFrom, IlEmitter itemToRemove)
            : base(preceedingCode)
        {
            _itemToRemoveFrom = itemToRemoveFrom;
            _itemToRemove = itemToRemove;
            _delegateRemoveMethodRef = LoadDelegateRemoveMethodDefinition();
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding()
                    .Concat(_itemToRemoveFrom.Emit())
                    .Concat(_itemToRemove.Emit())
                    .Concat(new [] { Instruction.Create(OpCodes.Call, _delegateRemoveMethodRef) } );
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
