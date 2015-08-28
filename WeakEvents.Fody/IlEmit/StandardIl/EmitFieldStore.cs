using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit.StandardIl
{
    class EmitFieldStore : IlEmitterBase
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
        public static IlEmitter StoreField(this MethodDefinition method, IlEmitter fieldValue, FieldReference field)
        {
            return StoreField(new EmptyEmitter(method), fieldValue, field);
        }
    }
}
