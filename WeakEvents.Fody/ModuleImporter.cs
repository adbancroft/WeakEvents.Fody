using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using WeakEvents.Runtime;

namespace WeakEvents.Fody
{
    // Central place to import items into the module.
    class ModuleImporter
    {
        private readonly ModuleDefinition _moduleDef;
        private readonly Lazy<CustomAttribute> _compilerGeneratedAttribute;
        private readonly Lazy<MethodReference> _openActionTCtor;
        private readonly Lazy<TypeReference> _openEventHandlerT;
        private readonly Lazy<MethodReference> _delegateRemove;
        private readonly Lazy<MethodReference> _getTypeFromHandle;
        private readonly Lazy<MethodReference> _delegateConvertChangeType;
        private readonly Lazy<MethodReference> _openFindWeakT;
        private readonly Lazy<MethodReference> _loadOpenMakeWeakT;

        public ModuleImporter(ModuleDefinition moduleDef)
        {
            _moduleDef = moduleDef;
            _compilerGeneratedAttribute = new Lazy<CustomAttribute>(LoadCompilerGeneratedAttribute);
            _openActionTCtor = new Lazy<MethodReference>(LoadOpenActionTConstructor);
            _openEventHandlerT = new Lazy<TypeReference>(LoadOpenEventHandlerT);
            _delegateRemove = new Lazy<MethodReference>(LoadDelegateRemoveMethodDefinition);
            _getTypeFromHandle = new Lazy<MethodReference>(LoadGetTypeFromHandle);
            _delegateConvertChangeType = new Lazy<MethodReference>(LoadDelegateConvertChangeType);
            _openFindWeakT = new Lazy<MethodReference>(LoadOpenFindWeakT);
            _loadOpenMakeWeakT = new Lazy<MethodReference>(LoadOpenMakeWeakT);
        }

        // CompilerGeneratedAttribute
        public CustomAttribute CompilerGeneratedAttribute { get { return _compilerGeneratedAttribute.Value; } }

        // Action<T>.ctor()
        public MethodReference ActionOpenCtor { get { return _openActionTCtor.Value; } }

        // EventHandler<T>
        public TypeReference EventHandlerT { get { return _openEventHandlerT.Value; } }

        // EventHandler<genericArg>
        public GenericInstanceType GetClosedEventHandlerT(TypeReference genericArg)
        {
            TypeReference eventArgsType = _moduleDef.Import(genericArg);
            return EventHandlerT.MakeGenericInstanceType(eventArgsType);
        }
        
        // Delegate.Remove
        public MethodReference DelegateRemove { get { return _delegateRemove.Value; } }

        // Type.GetTypeFromHandle (typeof)
        public MethodReference GetTypeFromHandle { get { return _getTypeFromHandle.Value; } }

        // WeakEvents.Runtime.DelegateConvert.ChangeType
        public MethodReference DelegateConvertChangeType { get { return _delegateConvertChangeType.Value; } }

        // WeakEvents.Runtime.WeakEventHandlerExtensions.FindWeak<T>
        public MethodReference OpenFindWeakT { get { return _openFindWeakT.Value; } }

        // WeakEvents.Runtime.WeakEventHandlerExtensions.MakeWeak<T>
        public MethodReference OpenMakeWeakT { get { return _loadOpenMakeWeakT.Value; } }        
        
        private CustomAttribute LoadCompilerGeneratedAttribute()
        {
            var compilerGeneratedDefinition = _moduleDef.Import(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
            var compilerGeneratedCtor = _moduleDef.Import(compilerGeneratedDefinition.Resolve().Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0));
            return new CustomAttribute(compilerGeneratedCtor);
        }

        private MethodReference LoadOpenActionTConstructor()
        {
            var actionDefinition = _moduleDef.Import(typeof(System.Action<>)).Resolve();
            return _moduleDef.Import(actionDefinition.Methods.Single(x => x.IsConstructor));
        }

        private TypeReference LoadOpenEventHandlerT()
        {
            return _moduleDef.Import(typeof(System.EventHandler<>));
        }

        private MethodReference LoadDelegateRemoveMethodDefinition()
        {
            System.Reflection.MethodInfo remove = typeof(Delegate).GetMethod("Remove", new [] { typeof(Delegate), typeof(Delegate) });
            return _moduleDef.Import(remove);
        }

        private MethodReference LoadGetTypeFromHandle()
        {
            System.Reflection.MethodInfo getTypeFromHandleReflect = typeof(Type).GetMethod("GetTypeFromHandle", new [] { typeof(RuntimeTypeHandle) });
            return _moduleDef.Import(getTypeFromHandleReflect);
        }

        private MethodReference LoadDelegateConvertChangeType()
        {
            System.Reflection.MethodInfo changeType = typeof(DelegateConvert).GetMethod("ChangeType", new [] { typeof(Delegate), typeof(Type) });
            return _moduleDef.Import(changeType);
        }

        private MethodReference LoadOpenFindWeakT()
        {
            System.Reflection.MethodInfo findWeak = typeof(WeakEventHandlerExtensions).GetMethod("FindWeak");
            return _moduleDef.Import(findWeak);
        }

        private MethodReference LoadOpenMakeWeakT()
        {
            System.Reflection.MethodInfo makeWeak = typeof(WeakEventHandlerExtensions).GetMethod("MakeWeak");
            return _moduleDef.Import(makeWeak);
        }

        private static string SysObjectName = typeof(System.Object).FullName;
        private static string IntPtrName = typeof(System.IntPtr).FullName;
    }
}
