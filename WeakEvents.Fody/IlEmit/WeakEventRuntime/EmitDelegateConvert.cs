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
            _inner =  Method
                        .Call(LoadDelegateConvertChangeType(), itemToConvert, Method.TypeOf(targetType))
                        .CastClass(targetType);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }

        private MethodReference LoadDelegateConvertChangeType()
        {
            var moduleDef = Method.Module;
            var classDef = moduleDef.Import(typeof(WeakEvents.Runtime.DelegateConvert)).Resolve();
            return moduleDef.Import(
                classDef.Methods
                    .Single(x =>
                          x.Name.Equals("ChangeType")
                       && x.HasParameters
                       && x.Parameters.Count == 2
                       && x.Parameters[0].ParameterType.FullName.Equals(DelegateName)
                       && x.Parameters[1].ParameterType.FullName.Equals(TypeName)));
        }

        private static string DelegateName = typeof(System.Delegate).FullName;
        private static string TypeName = typeof(System.Type).FullName;
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter DelegateConvert(this IlEmitter preceedingCode, IlEmitter itemToConvert, TypeReference targetType)
        {
            return new EmitDelegateConvert(preceedingCode, itemToConvert, targetType);
        }
        public static IlEmitter DelegateConvert(this MethodDefinition method, IlEmitter itemToConvert, TypeReference targetType)
        {
            return DelegateConvert(new EmptyEmitter(method), itemToConvert, targetType);
        }
    }
}
