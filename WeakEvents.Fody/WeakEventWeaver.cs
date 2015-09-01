using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using WeakEvents.Fody.IlEmit;
using WeakEvents.Fody.IlEmit.Other;
using WeakEvents.Fody.IlEmit.StandardIl;
using WeakEvents.Fody.IlEmit.WeakEventRuntime;

namespace WeakEvents.Fody
{
    class WeakEventWeaver
    {
        private readonly FieldDefinition _eventDelegate;
        private readonly GenericInstanceType _closedGenericEventHandler;
        private readonly bool _isGenericHandler;
        private readonly ModuleImporter _moduleimporter;

        public WeakEventWeaver(FieldReference eventDelegate, ModuleImporter moduleimporter)
        {
            _eventDelegate = eventDelegate.Resolve();
            _moduleimporter = moduleimporter;
            _closedGenericEventHandler = GetEquivalentGenericEventHandler(eventDelegate);
            _isGenericHandler = _closedGenericEventHandler.FullName.Equals(eventDelegate.FieldType.FullName);
        }

        // Addes a new method to the class that can unsubscribe a weak event handler from the event delegate
        // E.g.
        //      [CompilerGenerated]
        //      private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
        //      {
        //          this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
        //      }
        // This is used as a call back by the weak event handler to clean up when the target is garbage collected.
        public MethodDefinition AddUnsubscribeMethod()
        {
            // private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
            string unsubscribeMethodName = string.Format("<{0}>_Weak_Unsubscribe", _eventDelegate.Name);
            MethodDefinition unsubscribe = new MethodDefinition(unsubscribeMethodName, GetUnsubscribeMethodAttributes(), _eventDelegate.Module.TypeSystem.Void);
            unsubscribe.Parameters.Add(new ParameterDefinition(_closedGenericEventHandler));

            // [CompilerGenerated]
            unsubscribe.CustomAttributes.Add(_moduleimporter.CompilerGeneratedAttribute);

            _eventDelegate.DeclaringType.Methods.Add(unsubscribe);

            var weakHandler = unsubscribe.LoadMethod1stArg();
            if (!_isGenericHandler)
            {
                weakHandler = unsubscribe.DelegateConvert(weakHandler, _eventDelegate.FieldType);
            }
            var removeFromFieldDelegate = unsubscribe.CallDelegateRemove(unsubscribe.LoadField(_eventDelegate), weakHandler);
            var compatibleHandler = removeFromFieldDelegate;
            if (!_isGenericHandler)
            {
                compatibleHandler = unsubscribe.DelegateConvert(removeFromFieldDelegate, _eventDelegate.FieldType);
            }
            var instructions = unsubscribe.StoreField(compatibleHandler, _eventDelegate).Return();
            unsubscribe.InsertInstructions(instructions, 0);

            return unsubscribe;
        }

        // <event type> b = (<event type>)FindWeak(<source delegate>, (EventHandler< eventargsType >)value);
        public IlEmitter GenerateFindWeakIl(MethodDefinition method, VariableDefinition weakEventHandler)
        {
            var handler = method.LoadMethod1stArg();
            if (!_isGenericHandler)
            {
                handler = method.DelegateConvert(handler, _closedGenericEventHandler);
            }
            var callFindWeak = method.FindWeak(_closedGenericEventHandler, method.LoadField(_eventDelegate), handler);

            if (!_isGenericHandler)
            {
                return method.Store(weakEventHandler, method.DelegateConvert(callFindWeak, _eventDelegate.FieldType));
            }
            return method.Store(weakEventHandler, callFindWeak);
        }

        // Wrap the method parameter in a weak event handler and store in a variable.
        // i.e. <event type> b = (EventHandler)MakeWeak((EventHandler< eventargsType >)value, new Action<(EventHandler< eventargsType >)>(this.<woven unsubscribe action>));
        public IlEmitter GenerateMakeWeakIl(MethodDefinition method, MethodDefinition unsubscribe, VariableDefinition weakEventHandler)
        {
            var unsubscribeAction = method.NewObject(_moduleimporter.ActionOpenCtor.MakeDeclaringTypeClosedGeneric(_closedGenericEventHandler), method.LoadMethod(unsubscribe));
            IlEmitter genericHandler = method.LoadMethod1stArg();
            if (!_isGenericHandler)
            {
                genericHandler = method.DelegateConvert(genericHandler, _closedGenericEventHandler);
            }
            var genericWeakHandler = method.MakeWeak(_closedGenericEventHandler, genericHandler, unsubscribeAction);

            if (!_isGenericHandler)
            {
                return method.Store(weakEventHandler, method.DelegateConvert(genericWeakHandler, _eventDelegate.FieldType));
            }
            return method.Store(weakEventHandler, genericWeakHandler);
        }

        public VariableDefinition CreateEventHandlerVariable(MethodDefinition method)
        {
            return method.CreateVariable(_eventDelegate.FieldType);
        }

        private MethodAttributes GetUnsubscribeMethodAttributes()
        {
            var attributes = MethodAttributes.Private | MethodAttributes.HideBySig;
            if (_eventDelegate.IsStatic)
                attributes = attributes | MethodAttributes.Static;
            return attributes;
        }

        private GenericInstanceType GetEquivalentGenericEventHandler(FieldReference eventDelegate)
        {
            var moduleDef = eventDelegate.DeclaringType.Module;
            TypeReference eventArgsType = moduleDef.Import(eventDelegate.FieldType.GetEventArgsType());
            return _moduleimporter.EventHandlerT.MakeGenericInstanceType(eventArgsType);
        }
    }
}
