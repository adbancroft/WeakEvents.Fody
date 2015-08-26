using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody
{
    // Methods that weave calls to WeakEvents.Runtime.DelegateConvert.ChangeType()
    internal class DelegateConvertWeaver
    {
        // WeakEvents.Runtime.DelegateConvert.ChangeType()
        private MethodReference _delegateConvertChangeType;
        // Type.GetTypeFromHandle()
        private MethodReference _getTypeFromHandle;

        public DelegateConvertWeaver(ModuleDefinition moduleDef)
        {
            _delegateConvertChangeType = LoadDelegateConvertChangeType(moduleDef);
            _getTypeFromHandle = LoadGetTypeFromHandle(moduleDef);
        }

        // Inserts a call to DelegateConvert.ChangeType() at the given point in the instruction list.
        // The value being converted should be the previous item on the stack.
        // e.g. (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
        public int InsertChangeTypeCall(IList<Instruction> instructions, int insertPoint, TypeReference targetType)
        {
            foreach (var i in GetDelegateCastIl(targetType))
            {
                instructions.Insert(insertPoint, i);
                ++insertPoint;
            }

            return insertPoint;
        }

        // Adds a call to DelegateConvert.ChangeType() to the instruction list.
        // The value being converted should be the previous item on the stack.
        // e.g. (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
        public void AddChangeTypeCall(ICollection<Instruction> instructions, TypeReference targetType)
        {
            foreach (var i in GetDelegateCastIl(targetType))
            {
                instructions.Add(i);
            }
        }

        // Creates the IL necessary to cast delegates. This cannot be done directly
        // using the Castclass instruction. Instead we have to call the weak event runtime
        // WeakEvents.Runtime.DelegateConvert.ChangeType() method.
        private IEnumerable<Instruction> GetDelegateCastIl(TypeReference targetType)
        {
            // (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
            //                                           ^^^^^ <-- Already on the stack

            return new List<Instruction>() {
                // (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
                //                                                         ^^^^^^^^^^^^^
                Instruction.Create(OpCodes.Ldtoken, targetType),
                // (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
                //                                                  ^^^^^^
                Instruction.Create(OpCodes.Call, _getTypeFromHandle),
                // (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
                //                ^^^^^^^^^^^^^^^^^^^^^^^^^^
                Instruction.Create(OpCodes.Call, _delegateConvertChangeType),
                // (<target type>)DelegateConvert.ChangeType(value, typeof(<target type>));
                // ^^^^^^^^^^^^^^^
                Instruction.Create(OpCodes.Castclass, targetType)
            };
        }

        private static MethodReference LoadDelegateConvertChangeType(ModuleDefinition moduleDef)
        {
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

        private static MethodReference LoadGetTypeFromHandle(ModuleDefinition moduleDef)
        {
            System.Reflection.MethodInfo getTypeFromHandleReflect = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            return moduleDef.Import(getTypeFromHandleReflect);
        }

        private static string DelegateName = typeof(System.Delegate).FullName;
        private static string TypeName = typeof(System.Type).FullName;
    }
}
