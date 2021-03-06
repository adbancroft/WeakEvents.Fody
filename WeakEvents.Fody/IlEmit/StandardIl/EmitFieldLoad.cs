﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Ldfld (or Ldsfld is method is static)
    internal class EmitFieldLoad : IlEmitterBase
    {
        private readonly FieldReference _field;

        public EmitFieldLoad(ILEmitter preceedingCode, FieldReference field)
            : base(preceedingCode)
        {
            _field = field;
        }

        public override IEnumerable<Instruction> Emit()
        {
            if (Method.IsStatic)
            {
                return EmitPreceeding().Concat(new[] {
                    Instruction.Create(OpCodes.Ldsfld, _field)
                });
            }

            return EmitPreceeding().Concat(new[] {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldfld, _field)
            });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter LoadField(this ILEmitter preceedingCode, FieldReference field)
        {
            return new EmitFieldLoad(preceedingCode, field);
        }
    }
}