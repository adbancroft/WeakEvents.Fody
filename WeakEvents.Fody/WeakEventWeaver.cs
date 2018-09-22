using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Globalization;
using WeakEvents.Fody.IlEmit;
using WeakEvents.Fody.IlEmit.Other;
using WeakEvents.Fody.IlEmit.StandardIl;
using WeakEvents.Fody.IlEmit.WeakEventRuntime;

namespace WeakEvents.Fody
{
    internal class WeakEventWeaver
    {
        private readonly FieldDefinition _eventDelegate;
        private readonly GenericInstanceType _closedGenericEventHandler;
        private readonly bool _isGenericHandler;
        private readonly ModuleImporter _moduleimporter;

        public WeakEventWeaver(FieldReference eventDelegate, ModuleImporter moduleimporter)
        {
            _eventDelegate = eventDelegate.Resolve();
            _moduleimporter = moduleimporter;
            _closedGenericEventHandler = moduleimporter.GetClosedEventHandlerT(eventDelegate.FieldType.GetEventArgsType());
            _isGenericHandler = _closedGenericEventHandler.FullName.Equals(eventDelegate.FieldType.FullName);
        }

        // Addes a new method to the class that can unsubscribe a weak event handler from the event delegate
        // E.g.
#pragma warning disable S125 // Sections of code should not be "commented out"
        //      [CompilerGenerated]
        //      private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
        //      {
        //          this.EventDelegate = (<event type>) Delegate.Remove(this.EventDelegate, (<event type>)weh);
        //      }
#pragma warning restore S125 // Sections of code should not be "commented out"

        // This is used as a call back by the weak event handler to clean up when the target is garbage collected.
        public MethodDefinition AddUnsubscribeMethod()
        {
            // private void <event name>_Weak_Unsubscribe(EventHandler< eventargsType > weh)
            string unsubscribeMethodName = string.Format(CultureInfo.InvariantCulture, "<{0}>_Weak_Unsubscribe", _eventDelegate.Name);
#pragma warning disable CS0618 // Type or member is obsolete
            MethodDefinition unsubscribe = new MethodDefinition(unsubscribeMethodName, GetUnsubscribeMethodAttributes(), _eventDelegate.Module.TypeSystem.Void);
#pragma warning restore CS0618 // Type or member is obsolete
            unsubscribe.Parameters.Add(new ParameterDefinition(_closedGenericEventHandler));

            // [CompilerGenerated]
            unsubscribe.CustomAttributes.Add(_moduleimporter.CompilerGeneratedAttribute);

            _eventDelegate.DeclaringType.Methods.Add(unsubscribe);

            var rootEmitter = new EmptyEmitter(unsubscribe, _moduleimporter);
            var weakHandler = rootEmitter.LoadMethodFirstArg();
            if (!_isGenericHandler)
            {
                weakHandler = rootEmitter.DelegateConvert(weakHandler, _eventDelegate.FieldType);
            }
            var removeFromFieldDelegate = rootEmitter.CallDelegateRemove(rootEmitter.LoadField(_eventDelegate), weakHandler);
            var compatibleHandler = removeFromFieldDelegate;
            if (!_isGenericHandler)
            {
                compatibleHandler = rootEmitter.DelegateConvert(removeFromFieldDelegate, _eventDelegate.FieldType);
            }
            var instructions = rootEmitter.StoreField(compatibleHandler, _eventDelegate).Return();
            unsubscribe.InsertInstructions(instructions, 0);

            return unsubscribe;
        }

#pragma warning disable S125 // Sections of code should not be "commented out"
        // <event type> b = (<event type>)FindWeak(<source delegate>, (EventHandler< eventargsType >)value);
#pragma warning restore S125 // Sections of code should not be "commented out"

        public ILEmitter GenerateFindWeakIl(MethodDefinition method, VariableDefinition weakEventHandler)
        {
            var rootEmitter = new EmptyEmitter(method, _moduleimporter);

            var handler = rootEmitter.LoadMethodFirstArg();
            if (!_isGenericHandler)
            {
                handler = rootEmitter.DelegateConvert(handler, _closedGenericEventHandler);
            }
            var callFindWeak = rootEmitter.FindWeak(_closedGenericEventHandler, rootEmitter.LoadField(_eventDelegate), handler);

            if (!_isGenericHandler)
            {
                return rootEmitter.Store(weakEventHandler, rootEmitter.DelegateConvert(callFindWeak, _eventDelegate.FieldType));
            }
            return rootEmitter.Store(weakEventHandler, callFindWeak);
        }

        // Wrap the method parameter in a weak event handler and store in a variable.
#pragma warning disable S125 // Sections of code should not be "commented out"
        // i.e. <event type> b = (EventHandler)MakeWeak((EventHandler< eventargsType >)value, new Action<(EventHandler< eventargsType >)>(this.<woven unsubscribe action>));
#pragma warning restore S125 // Sections of code should not be "commented out"

        public ILEmitter GenerateMakeWeakIl(MethodDefinition method, MethodDefinition unsubscribe, VariableDefinition weakEventHandler)
        {
            var rootEmitter = new EmptyEmitter(method, _moduleimporter);

            var unsubscribeAction = rootEmitter.NewObject(_moduleimporter.ActionOpenCtor.MakeDeclaringTypeClosedGeneric(_closedGenericEventHandler), rootEmitter.LoadMethod(unsubscribe));
            ILEmitter genericHandler = rootEmitter.LoadMethodFirstArg();
            if (!_isGenericHandler)
            {
                genericHandler = rootEmitter.DelegateConvert(genericHandler, _closedGenericEventHandler);
            }
            var genericWeakHandler = rootEmitter.MakeWeak(_closedGenericEventHandler, genericHandler, unsubscribeAction);

            if (!_isGenericHandler)
            {
                return rootEmitter.Store(weakEventHandler, rootEmitter.DelegateConvert(genericWeakHandler, _eventDelegate.FieldType));
            }
            return rootEmitter.Store(weakEventHandler, genericWeakHandler);
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
    }
}