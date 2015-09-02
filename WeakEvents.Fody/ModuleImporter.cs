using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

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
            return _moduleDef.Import(
                actionDefinition.Methods
                    .Single(x =>
                        x.IsConstructor &&
                        x.Parameters.Count == 2 &&
                        x.Parameters[0].ParameterType.FullName == SysObjectName &&
                        x.Parameters[1].ParameterType.FullName == IntPtrName));
        }

        private TypeReference LoadOpenEventHandlerT()
        {
            return _moduleDef.Import(typeof(System.EventHandler<>));
        }

        private MethodReference LoadDelegateRemoveMethodDefinition()
        {
            var delegateReference = _moduleDef.Import(typeof(System.Delegate));
            var delegateDefinition = delegateReference.Resolve();
            var methodDefinition = delegateDefinition.Methods
                .Single(x =>
                    x.Name == "Remove" &&
                    x.Parameters.Count == 2 &&
                    x.Parameters.All(p => p.ParameterType == delegateDefinition));
            return _moduleDef.Import(methodDefinition);
        }

        private MethodReference LoadGetTypeFromHandle()
        {
            System.Reflection.MethodInfo getTypeFromHandleReflect = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            return _moduleDef.Import(getTypeFromHandleReflect);
        }

        private MethodReference LoadDelegateConvertChangeType()
        {
            var classDef = _moduleDef.Import(typeof(WeakEvents.Runtime.DelegateConvert)).Resolve();
            return _moduleDef.Import(
                classDef.Methods
                    .Single(x =>
                          x.Name.Equals("ChangeType")
                       && x.HasParameters
                       && x.Parameters.Count == 2
                       && x.Parameters[0].ParameterType.FullName.Equals(DelegateName)
                       && x.Parameters[1].ParameterType.FullName.Equals(TypeName)));
        }

        private MethodReference LoadOpenFindWeakT()
        {
            var wehExtensionsReference = _moduleDef.Import(typeof(WeakEvents.Runtime.WeakEventHandlerExtensions));
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
            return _moduleDef.Import(makeWeakMethodDefinition);
        }

        private MethodReference LoadOpenMakeWeakT()
        {
            var wehExtensionsReference = _moduleDef.Import(typeof(WeakEvents.Runtime.WeakEventHandlerExtensions));
            var wehExtensionsDefinition = wehExtensionsReference.Resolve();
            var makeWeakMethodDefinition = wehExtensionsDefinition.Methods.Single(
                x => x.Name == "MakeWeak"
                    && x.CallingConvention == MethodCallingConvention.Generic
                    && x.HasGenericParameters
                    && x.GenericParameters[0].HasConstraints
                    && x.GenericParameters[0].Constraints[0].FullName.Equals(SysEventArgsName)
            );
            return _moduleDef.Import(makeWeakMethodDefinition);
        }

        private static string SysEventArgsName = typeof(System.EventArgs).FullName; 
        private static string DelegateName = typeof(System.Delegate).FullName;
        private static string TypeName = typeof(System.Type).FullName;
        private static string SysObjectName = typeof(System.Object).FullName;
        private static string IntPtrName = typeof(System.IntPtr).FullName;
    }
}
