using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace WeakEvents.Fody
{
    class WeakEventWeaver
    {
        private ModuleDefinition _moduleDef;
        private ILogger _logger;
        private DelegateConvertWeaver _delegateCastWeaver;

        // Delegate.Remove()
        private MethodReference _delegateRemoveMethodRef;
        // CompilerGeneratedAttribute
        private CustomAttribute _compilerGeneratedAttribute;
        // WeakEvents.Runtime.WeakEventHandlerExtensions.MakeWeak<T>()
        private MethodReference _openMakeWeakT;
        // WeakEvents.Runtime.WeakEventHandlerExtensions.FindWeak<T>()
        private MethodReference _openFindWeakT;
        // Action<T>.ctor()
        private MethodReference _openActionTCtor;
        // EventHandler<T>
        private TypeReference _openEventHandlerT;

        public WeakEventWeaver(ModuleDefinition moduleDef, ILogger logger)
        {
            _moduleDef = moduleDef;
            _logger = logger;
            _delegateCastWeaver = new DelegateConvertWeaver(moduleDef);

            _delegateRemoveMethodRef = LoadDelegateRemoveMethodDefinition(moduleDef);
            _compilerGeneratedAttribute = LoadCompilerGeneratedAttribute(moduleDef);
            _openMakeWeakT = LoadOpenMakeWeakT(moduleDef);
            _openFindWeakT = LoadOpenFindWeakT(moduleDef);
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
            var makeWeak = WeaveFindWeakCall(eventt.RemoveMethod, eventDelegate);

            // Now replace any further use of the method parameter (Ldarg_1) with the weak event handler
            var instructions = eventt.RemoveMethod.Body.Instructions;
            for (int i = makeWeak.Item2; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(Code.Ldarg_1))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, makeWeak.Item1);
                }
            }
        }

        // <event type> b = (<event type>)FindWeak(<source delegate>, (EventHandler< eventargsType >)value);
        private Tuple<VariableDefinition, int> WeaveFindWeakCall(MethodDefinition method, FieldReference eventDelegate)
        {
            var closedEventHandlerT = GetEquivalentGenericEventHandler(eventDelegate);

            var instructions = method.Body.Instructions;
            int insertPoint = 0;

            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                                         ^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldarg_0)); insertPoint++;
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                                              ^^^^^^^^^^^^^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldfld, eventDelegate)); insertPoint++;
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                                                                                                ^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldarg_1)); insertPoint++;
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                                                                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            insertPoint = _delegateCastWeaver.InsertChangeTypeCall(instructions, insertPoint, closedEventHandlerT);
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                                ^^^^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Call, _openFindWeakT.MakeMethodClosedGeneric(closedEventHandlerT.GenericArguments[0]))); insertPoint++;
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            //                  ^^^^^^^^^^^^^^
            insertPoint = _delegateCastWeaver.InsertChangeTypeCall(instructions, insertPoint, eventDelegate.FieldType);
            // <event type> b = (<event type>)FindWeak(this.<source delegate>, (EventHandler< eventargsType >)value);
            // ^^^^^^^^^^^^^^
            return InsertVariableStorage(method, insertPoint, eventDelegate.FieldType);
        }

        private void ProcessAddMethod(EventDefinition eventt, FieldReference eventDelegate)
        {
            var makeWeak = WeaveMakeWeakCall(eventt.AddMethod, eventDelegate, AddUnsubscribeMethodForEvent(eventt, eventDelegate));

            // Now replace any further use of the method parameter (Ldarg_1) with the weak event handler
            var instructions = eventt.AddMethod.Body.Instructions;
            for (int i = makeWeak.Item2; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode.Code.Equals(Code.Ldarg_1))
                {
                    instructions[i] = Instruction.Create(OpCodes.Ldloc, makeWeak.Item1);
                }
            }
        }

        // Wrap the method parameter in a weak event handler and store in a variable.
        // i.e. <event type> b = (EventHandler)MakeWeak((EventHandler< eventargsType >)value, new Action<(EventHandler< eventargsType >)>(this.<woven unsubscribe action>));
        private Tuple<VariableDefinition, int> WeaveMakeWeakCall(MethodDefinition method, FieldReference eventDelegate, MethodDefinition unsubscribe)
        {
            var closedEventHandlerT = GetEquivalentGenericEventHandler(eventDelegate);

            var instructions = method.Body.Instructions;
            int insertPoint = 0;

            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                                                        ^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldarg_1)); insertPoint++;
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            insertPoint = _delegateCastWeaver.InsertChangeTypeCall(instructions, insertPoint, closedEventHandlerT);
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                                                                                                         ^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldarg_0)); insertPoint++;
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                                                                                                              ^^^^^^^^^^^^^^^^^^^^^^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Ldftn, unsubscribe)); insertPoint++;
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                                                               ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Newobj, _openActionTCtor.MakeDeclaringTypeClosedGeneric(closedEventHandlerT))); insertPoint++;
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                                ^^^^^^^^
            instructions.Insert(insertPoint, Instruction.Create(OpCodes.Call, _openMakeWeakT.MakeMethodClosedGeneric(closedEventHandlerT.GenericArguments[0]))); insertPoint++;
            // <event type> b = (<event type>)MakeWeak((EventHandler< eventargsType >)value, new Action<EventHandler< eventargsType >>(this.<woven unsubscribe action>));
            //                  ^^^^^^^^^^^^^^
            insertPoint = _delegateCastWeaver.InsertChangeTypeCall(instructions, insertPoint, eventDelegate.FieldType);
            // <event type> b = MakeWeak(value, new Action<<event type>>(this.<woven unsubscribe action>));
            // ^^^^^^^^^^^^^^^^
            return InsertVariableStorage(method, insertPoint, eventDelegate.FieldType);
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
            MethodDefinition unsubscribe = new MethodDefinition(unsubscribeMethodName, MethodAttributes.Private | MethodAttributes.HideBySig, _moduleDef.TypeSystem.Void);
            unsubscribe.Parameters.Add(new ParameterDefinition(closedEventHandlerT));

            // [CompilerGenerated]
            unsubscribe.CustomAttributes.Add(_compilerGeneratedAttribute);

            var instructions = unsubscribe.Body.Instructions;
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            //                                                     ^^^^^^^^^^^^^^^^^^
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Dup));
            instructions.Add(Instruction.Create(OpCodes.Ldfld, eventDelegate));
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            //                                                                                       ^^^
            instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            //                                                                         ^^^^^^^^^^^^^^
            _delegateCastWeaver.AddChangeTypeCall(instructions, eventDelegate.FieldType);
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            //                                     ^^^^^^^^^^^^^^^
            instructions.Add(Instruction.Create(OpCodes.Call, _delegateRemoveMethodRef));
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            //                      ^^^^^^^^^^^^^^
            _delegateCastWeaver.AddChangeTypeCall(instructions, eventDelegate.FieldType);
            // this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
            // ^^^^^^^^^^^^^^^^^^^^
            instructions.Add(Instruction.Create(OpCodes.Stfld, eventDelegate));

            instructions.Add(Instruction.Create(OpCodes.Ret));
            eventt.DeclaringType.Methods.Add(unsubscribe);

            return unsubscribe;
        }

        private Tuple<VariableDefinition, int> InsertVariableStorage(MethodDefinition method, int insertIndex, TypeReference variableType)
        {
            var variable = new VariableDefinition(variableType);
            method.Body.Variables.Add(variable);
            method.Body.Instructions.Insert(insertIndex, Instruction.Create(OpCodes.Stloc, variable)); insertIndex++;

            return Tuple.Create(variable, insertIndex);
        }

        private GenericInstanceType GetEquivalentGenericEventHandler(FieldReference eventDelegate)
        {
            TypeReference eventArgsType = _moduleDef.Import(eventDelegate.FieldType.GetEventArgsType());
            return _openEventHandlerT.MakeGenericInstanceType(eventArgsType);
        }

        #region Static type & method loaders

        private static MethodReference LoadDelegateRemoveMethodDefinition(ModuleDefinition moduleDef)
        {
            var delegateReference = moduleDef.Import(typeof(System.Delegate));
            var delegateDefinition = delegateReference.Resolve();
            var methodDefinition = delegateDefinition.Methods
                .Single(x =>
                    x.Name == "Remove" &&
                    x.Parameters.Count == 2 &&
                    x.Parameters.All(p => p.ParameterType == delegateDefinition));
            return moduleDef.Import(methodDefinition);
        }

        private static CustomAttribute LoadCompilerGeneratedAttribute(ModuleDefinition moduleDef)
        {
            var compilerGeneratedDefinition = moduleDef.Import(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
            var compilerGeneratedCtor = moduleDef.Import(compilerGeneratedDefinition.Resolve().Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0));
            return new CustomAttribute(compilerGeneratedCtor);
        }

        private static MethodReference LoadOpenMakeWeakT(ModuleDefinition moduleDef)
        {
            var wehExtensionsReference = moduleDef.Import(typeof(WeakEvents.Runtime.WeakEventHandlerExtensions));
            var wehExtensionsDefinition = wehExtensionsReference.Resolve();
            var makeWeakMethodDefinition = wehExtensionsDefinition.Methods.Single(
                x => x.Name == "MakeWeak"
                    && x.CallingConvention == MethodCallingConvention.Generic
                    && x.HasGenericParameters
                    && x.GenericParameters[0].HasConstraints
                    && x.GenericParameters[0].Constraints[0].FullName.Equals(SysEventArgsName)
            );
            return moduleDef.Import(makeWeakMethodDefinition);
        }

        private static MethodReference LoadOpenFindWeakT(ModuleDefinition moduleDef)
        {
            var wehExtensionsReference = moduleDef.Import(typeof(WeakEvents.Runtime.WeakEventHandlerExtensions));
            var wehExtensionsDefinition = wehExtensionsReference.Resolve();
            var makeWeakMethodDefinition = wehExtensionsDefinition.Methods.Single(
                x => x.Name == "FindWeak"
                    && x.HasParameters
                    && x.Parameters.Count == 2
                    && x.Parameters[0].ParameterType.FullName.Equals(DelegateName)
                    && x.CallingConvention == MethodCallingConvention.Generic
                    && x.HasGenericParameters
                    && x.GenericParameters[0].HasConstraints
                    && x.GenericParameters[0].Constraints[0].FullName.Equals(SysEventArgsName)
            );
            return moduleDef.Import(makeWeakMethodDefinition);
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
        private static string SysEventArgsName = typeof(System.EventArgs).FullName;
        private static string DelegateName = typeof(System.Delegate).FullName;

        #endregion
    }
}
