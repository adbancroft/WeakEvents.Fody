using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    // OpCodes.Stfld (or Stsfld is method is static)
    internal class EmitFieldStore : IlEmitterBase
    {
        private readonly IlEmitter _fieldValue;
        private readonly FieldReference _field;

        public EmitFieldStore(IlEmitter preceedingCode, FieldReference field, IlEmitter fieldValue)
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
        public static IlEmitter StoreField(this IlEmitter preceedingCode, IlEmitter fieldValue, FieldReference field)
        {
            return new EmitFieldStore(preceedingCode, field, fieldValue);
        }
    }
}