using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody
{
    internal class EventWeaver
    {
        private readonly ModuleDefinition _moduleDef;
        private readonly ILogger _logger;
        private readonly ModuleImporter _moduleImporter;

        public EventWeaver(ModuleDefinition moduleDef, ILogger logger)
        {
            _moduleDef = moduleDef;
            _logger = logger;
            _moduleImporter = new ModuleImporter(moduleDef);
        }

        public void ProcessEvent(EventDefinition eventt)
        {
            _logger.LogDebug("Beginning processing event " + eventt.Name);

            FieldReference eventDelegate = eventt.GetEventDelegate();

            if (eventDelegate != null)
            {
                if (eventDelegate.FieldType.IsValidEventDelegate())
                {
                    WeakEventWeaver weakEventWeaver = new WeakEventWeaver(eventDelegate, _moduleImporter);
                    ProcessAddMethod(eventt.AddMethod, weakEventWeaver);
                    ProcessRemoveMethod(eventt.RemoveMethod, weakEventWeaver);
                }
                else
                {
                    _logger.LogInfo("Skipping event " + eventt + ", incompatible event delegate type");
                }
            }
            else
            {
                _logger.LogInfo("Skipping event " + eventt + ", could not determine the event delegate field");
            }

            _logger.LogDebug("Finished processing event " + eventt.Name);
        }

        private void ProcessRemoveMethod(MethodDefinition removeMethod, WeakEventWeaver weakEventWeaver)
        {
            var weakEventHandler = weakEventWeaver.CreateEventHandlerVariable(removeMethod);
            var makeWeak = weakEventWeaver.GenerateFindWeakIl(removeMethod, weakEventHandler);
            int oldCodeIndex = removeMethod.InsertInstructions(makeWeak, 0);

            // Now replace any further use of the method parameter (Ldarg_1, or Ldarg_0 if static) with the weak event handler
            var instructionToReplace = GetMethodLoad1stArgumentCode(removeMethod);
            var instructions = removeMethod.Body.Instructions;
            for (int i = oldCodeIndex; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(instructionToReplace))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, weakEventHandler);
                }
            }
        }

        private void ProcessAddMethod(MethodDefinition addMethod, WeakEventWeaver weakEventWeaver)
        {
            var weakEventHandler = weakEventWeaver.CreateEventHandlerVariable(addMethod);
            var makeWeak = weakEventWeaver.GenerateMakeWeakIl(addMethod, weakEventWeaver.AddUnsubscribeMethod(), weakEventHandler);
            int oldCodeIndex = addMethod.InsertInstructions(makeWeak, 0);

            // Now replace any further use of the method parameter (Ldarg_1, or Ldarg_0 if static) with the weak event handler
            var instructionToReplace = GetMethodLoad1stArgumentCode(addMethod);
            var instructions = addMethod.Body.Instructions;
            for (int i = oldCodeIndex; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(instructionToReplace))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, weakEventHandler);
                }
            }
        }

        // The opcode to load a methods 1st real argument varies depending on the static modifier.
        private static Code GetMethodLoad1stArgumentCode(MethodDefinition method)
        {
            if (method.IsStatic)
            {
                return Code.Ldarg_0;
            }

            return Code.Ldarg_1;
        }
    }
}