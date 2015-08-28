using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.Other
{
    class EmitTypeOf : IlEmitterBase
    {
        private TypeReference _targetType;
        // Type.GetTypeFromHandle()
        private MethodReference _getTypeFromHandle;

        public EmitTypeOf(IlEmitter preceedingCode, TypeReference targetType)
            : base(preceedingCode)
        {
            _getTypeFromHandle = LoadGetTypeFromHandle();
            _targetType = targetType;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding()
                .Concat(new[] {
                    Instruction.Create(OpCodes.Ldtoken, _targetType),
                    Instruction.Create(OpCodes.Call, _getTypeFromHandle)
                });
        }

        private MethodReference LoadGetTypeFromHandle()
        {
            var moduleDef = Method.Module;
            System.Reflection.MethodInfo getTypeFromHandleReflect = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            return moduleDef.Import(getTypeFromHandleReflect);
        }
    }


    static partial class EmitterExtensions
    {
        public static IlEmitter TypeOf(this IlEmitter preceedingCode, TypeReference targetType)
        {
            return new EmitTypeOf(preceedingCode, targetType);
        }
        public static IlEmitter TypeOf(this MethodDefinition method, TypeReference targetType)
        {
            return TypeOf(new EmptyEmitter(method), targetType);
        }
    }
}
