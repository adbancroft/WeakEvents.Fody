using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    class EmitFieldLoad : IlEmitterBase
    {
        FieldReference _field;

        public EmitFieldLoad(IlEmitter preceedingCode, FieldReference field)
            : base(preceedingCode)
        {
            _field = field;
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(new[] { 
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldfld, _field)
            });
        }
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter LoadField(this IlEmitter preceedingCode, FieldReference field)
        {
            return new EmitFieldLoad(preceedingCode, field);
        }
        public static IlEmitter LoadField(this MethodDefinition method, FieldReference field)
        {
            return LoadField(new EmptyEmitter(method), field);
        }
    }
}
