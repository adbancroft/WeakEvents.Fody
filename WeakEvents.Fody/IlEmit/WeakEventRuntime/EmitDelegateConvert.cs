using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    class EmitDelegateConvert : IlEmitterBase
    {
        private TypeReference _targetType;
        private IlEmitter _itemToConvert;

        // WeakEvents.Runtime.DelegateConvert.ChangeType()
        private MethodReference _delegateConvertChangeType;
        // Type.GetTypeFromHandle()
        private MethodReference _getTypeFromHandle;

        public EmitDelegateConvert(IlEmitter preceedingCode, IlEmitter itemToConvert, TypeReference targetType)
            : base(preceedingCode)
        {
            _targetType = targetType;
            _itemToConvert = itemToConvert;
            _delegateConvertChangeType = LoadDelegateConvertChangeType();
            _getTypeFromHandle = LoadGetTypeFromHandle();
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding()
                .Concat(_itemToConvert.Emit())
                .Concat( new [] {
                    Instruction.Create(OpCodes.Ldtoken, _targetType),
                    Instruction.Create(OpCodes.Call, _getTypeFromHandle),
                    Instruction.Create(OpCodes.Call, _delegateConvertChangeType),
                    Instruction.Create(OpCodes.Castclass, _targetType)
                });
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

        private MethodReference LoadGetTypeFromHandle()
        {
            var moduleDef = Method.Module;
            System.Reflection.MethodInfo getTypeFromHandleReflect = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            return moduleDef.Import(getTypeFromHandleReflect);
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
