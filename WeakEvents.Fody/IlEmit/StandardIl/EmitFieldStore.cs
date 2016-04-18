using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Stfld (or Stsfld is method is static)
    internal class EmitFieldStore : IlEmitterBase
    {
        private readonly ILEmitter _fieldValue;
        private readonly FieldReference _field;

        public EmitFieldStore(ILEmitter preceedingCode, FieldReference field, ILEmitter fieldValue)
            : base(preceedingCode)
        {
            _fieldValue = fieldValue;
            _field = field;
        }

        public override IEnumerable<Instruction> Emit()
        {
            if (Method.IsStatic)
            {
                return EmitPreceeding()
                            .Concat(_fieldValue.Emit())
                            .Concat(new[] { Instruction.Create(OpCodes.Stsfld, _field) });
            }

            return EmitPreceeding()
                        .Concat(new[] { Instruction.Create(OpCodes.Ldarg_0) })
                        .Concat(_fieldValue.Emit())
                        .Concat(new[] { Instruction.Create(OpCodes.Stfld, _field) });
        }
    }

    static partial class EmitterExtensions
    {
        public static ILEmitter StoreField(this ILEmitter preceedingCode, ILEmitter fieldValue, FieldReference field)
        {
            return new EmitFieldStore(preceedingCode, field, fieldValue);
        }
    }
}