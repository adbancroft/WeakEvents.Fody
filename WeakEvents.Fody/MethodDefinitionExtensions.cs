using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit;

namespace WeakEvents.Fody
{
    internal static class MethodDefinitionExtensions
    {
        public static int InsertInstructions(this MethodDefinition method, IlEmitter weakHandler, int insertPoint)
        {
            foreach (var i in weakHandler.Emit())
            {
                method.Body.Instructions.Insert(insertPoint, i);
                ++insertPoint;
            }
            return insertPoint;
        }

        public static VariableDefinition CreateVariable(this MethodDefinition method, TypeReference variableType)
        {
            var variable = new VariableDefinition(variableType);
            method.Body.Variables.Add(variable);
            return variable;
        }
    }
}