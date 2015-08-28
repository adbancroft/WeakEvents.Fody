using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WeakEvents.Fody.IlEmit;
using WeakEvents.Fody.IlEmit.StandardIl;
using WeakEvents.Fody.IlEmit.WeakEventRuntime;
using WeakEvents.Fody.IlEmit.Other;

namespace WeakEvents.Fody
{
    class WeakEventWeaver
    {
        private ModuleDefinition _moduleDef;
        private ILogger _logger;

        // CompilerGeneratedAttribute
        private CustomAttribute _compilerGeneratedAttribute;
        // Action<T>.ctor()
        private MethodReference _openActionTCtor;
        // EventHandler<T>
        private TypeReference _openEventHandlerT;

        public WeakEventWeaver(ModuleDefinition moduleDef, ILogger logger)
        {
            _moduleDef = moduleDef;
            _logger = logger;

            _compilerGeneratedAttribute = LoadCompilerGeneratedAttribute(moduleDef);
            _openActionTCtor = LoadOpenActionTConstructor(moduleDef);
            _openEventHandlerT = LoadOpenEventHandlerT(moduleDef);
        }

        public void ProcessEvent(EventDefinition eventt)
        {
            _logger.LogDebug("Beginning processing event " + eventt.Name);

            FieldReference eventDelegate = eventt.GetEventDelegate();

            if (eventDelegate != null)
            {
                if (eventDelegate.FieldType.IsValidEventDelegate())
                {
                    ProcessAddMethod(eventt, eventDelegate);
                    ProcessRemoveMethod(eventt, eventDelegate);
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

        private void ProcessRemoveMethod(EventDefinition eventt, FieldReference eventDelegate)
        {
            var weakEventHandler = CreateVariable(eventt.RemoveMethod, eventDelegate.FieldType);
            var makeWeak = WeaveFindWeakCall(eventt.RemoveMethod, eventDelegate, weakEventHandler);
            int oldCodeIndex = InsertInstructions(eventt.RemoveMethod, makeWeak, 0);

            // Now replace any further use of the method parameter (Ldarg_1) with the weak event handler
            var instructions = eventt.RemoveMethod.Body.Instructions;
            for (int i = oldCodeIndex; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(Code.Ldarg_1))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, weakEventHandler);
                }
            }
        }

        // <event type> b = (<event type>)FindWeak(<source delegate>, (EventHandler< eventargsType >)value);
        private IlEmitter WeaveFindWeakCall(MethodDefinition method, FieldReference eventDelegate, VariableDefinition weakEventHandler)
        {
            var closedEventHandlerT = GetEquivalentGenericEventHandler(eventDelegate);

            var callFindWeak = method.FindWeak(closedEventHandlerT, method.LoadField(eventDelegate), method.DelegateConvert(method.LoadMethod1stArg(), closedEventHandlerT));
            var weakHandler = method.DelegateConvert(callFindWeak, eventDelegate.FieldType);

            return method.Store(weakEventHandler, weakHandler);
        }

        private static int InsertInstructions(MethodDefinition method, IlEmitter weakHandler, int insertPoint)
        {
            foreach (var i in weakHandler.Emit())
            {
                method.Body.Instructions.Insert(insertPoint, i);
                ++insertPoint;
            }
            return insertPoint;
        }

        private void ProcessAddMethod(EventDefinition eventt, FieldReference eventDelegate)
        {
            var weakEventHandler = CreateVariable(eventt.AddMethod, eventDelegate.FieldType);
            var makeWeak = WeaveMakeWeakCall(eventt.AddMethod, eventDelegate, AddUnsubscribeMethodForEvent(eventt, eventDelegate), weakEventHandler);
            int oldCodeIndex = InsertInstructions(eventt.AddMethod, makeWeak, 0);

            // Now replace any further use of the method parameter (Ldarg_1) with the weak event handler
            var instructions = eventt.AddMethod.Body.Instructions;
            for (int i = oldCodeIndex; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(Code.Ldarg_1))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, weakEventHandler);
                }
            }
        }

        // Wrap the method parameter in a weak event handler and store in a variable.
        // i.e. <event type> b = (EventHandler)MakeWeak((EventHandler< eventargsType >)value, new Action<(EventHandler< eventargsType >)>(this.<woven unsubscribe action>));
        private IlEmitter WeaveMakeWeakCall(MethodDefinition method, FieldReference eventDelegate, MethodDefinition unsubscribe, VariableDefinition weakEventHandler)
        {
            var closedEventHandlerT = GetEquivalentGenericEventHandler(eventDelegate);

            var unsubscribeAction = method.NewObject(_openActionTCtor.MakeDeclaringTypeClosedGeneric(closedEventHandlerT), method.LoadMethod(unsubscribe));
            var genericHandler = method.DelegateConvert(method.LoadMethod1stArg(), closedEventHandlerT);
            var genericWeakHandler = method.MakeWeak(closedEventHandlerT, genericHandler, unsubscribeAction);
            var weakHandler = method.DelegateConvert(genericWeakHandler, eventDelegate.FieldType);

            return method.Store(weakEventHandler, weakHandler);
        }

        // Addes a new method to the class that can unsubscribe an event handler from the event delegate
        // E.g.
        //      [CompilerGenerated]
        //      private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
        //      {
        //          this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
        //      }
        // This is used as a call back by the weak event handler to clean up when the target is garbage collected.
        private MethodDefinition AddUnsubscribeMethodForEvent(EventDefinition eventt, FieldReference eventDelegate)
        {
            var closedEventHandlerT = GetEquivalentGenericEventHandler(eventDelegate);

            // private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
            string unsubscribeMethodName = string.Format("<{0}>_Weak_Unsubscribe", eventt.AddMethod.Name);
            MethodDefinition unsubscribe = new MethodDefinition(unsubscribeMethodName, GetUnsubscribeMethodAttributes(eventt), _moduleDef.TypeSystem.Void);
            unsubscribe.Parameters.Add(new ParameterDefinition(closedEventHandlerT));

            // [CompilerGenerated]
            unsubscribe.CustomAttributes.Add(_compilerGeneratedAttribute);

            eventt.DeclaringType.Methods.Add(unsubscribe);

            var weakHandler = unsubscribe.DelegateConvert(unsubscribe.LoadMethod1stArg(), eventDelegate.FieldType);
            var removeFromFieldDelegate = unsubscribe.CallDelegateRemove(unsubscribe.LoadField(eventDelegate), weakHandler);
            var compatibleHandler = unsubscribe.DelegateConvert(removeFromFieldDelegate, eventDelegate.FieldType);
            var instructions = unsubscribe.StoreField(compatibleHandler, eventDelegate).Return();

            foreach (var i in instructions.Emit())
            {
                unsubscribe.Body.Instructions.Add(i);
            }

            return unsubscribe;
        }

        private static MethodAttributes GetUnsubscribeMethodAttributes(EventDefinition eventt)
        {
            var attributes = MethodAttributes.Private | MethodAttributes.HideBySig;
            if (eventt.AddMethod.IsStatic)
                attributes = attributes | MethodAttributes.Static;
            return attributes;
        }

        private static VariableDefinition CreateVariable(MethodDefinition method, TypeReference variableType)
        {
            var variable = new VariableDefinition(variableType);
            method.Body.Variables.Add(variable);
            return variable;
        }

        private GenericInstanceType GetEquivalentGenericEventHandler(FieldReference eventDelegate)
        {
            TypeReference eventArgsType = _moduleDef.Import(eventDelegate.FieldType.GetEventArgsType());
            return _openEventHandlerT.MakeGenericInstanceType(eventArgsType);
        }

        #region Static type & method loaders

        private static CustomAttribute LoadCompilerGeneratedAttribute(ModuleDefinition moduleDef)
        {
            var compilerGeneratedDefinition = moduleDef.Import(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
            var compilerGeneratedCtor = moduleDef.Import(compilerGeneratedDefinition.Resolve().Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0));
            return new CustomAttribute(compilerGeneratedCtor);
        }

        private static MethodReference LoadOpenActionTConstructor(ModuleDefinition moduleDef)
        {
            var actionDefinition = moduleDef.Import(typeof(System.Action<>)).Resolve();
            return moduleDef.Import(
                actionDefinition.Methods
                    .Single(x =>
                        x.IsConstructor &&
                        x.Parameters.Count == 2 &&
                        x.Parameters[0].ParameterType.FullName == SysObjectName &&
                        x.Parameters[1].ParameterType.FullName == IntPtrName));
        }

        private static TypeReference LoadOpenEventHandlerT(ModuleDefinition moduleDef)
        {
            return moduleDef.Import(typeof(System.EventHandler<>));
        }

        private static string SysObjectName = typeof(System.Object).FullName;
        private static string IntPtrName = typeof(System.IntPtr).FullName;

        #endregion
    }
}
