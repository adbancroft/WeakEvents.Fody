using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.Other
{
    // Emit IL for a call to typeof(targetType)
    class EmitTypeOf : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitTypeOf(IlEmitter preceedingCode, TypeReference targetType)
            : base(preceedingCode)
        {
            _inner = new EmptyEmitter(preceedingCode).Call(LoadGetTypeFromHandle(), new EmptyEmitter(preceedingCode).LdToken(targetType));
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
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
    }
}
