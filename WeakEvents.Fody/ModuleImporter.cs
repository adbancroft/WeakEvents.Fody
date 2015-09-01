using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace WeakEvents.Fody
{
    // Central place to import items into the module.
    class ModuleImporter
    {
        private readonly ModuleDefinition _moduleDef;
        private readonly Lazy<CustomAttribute> _compilerGeneratedAttribute;
        private readonly Lazy<MethodReference> _openActionTCtor;
        private readonly Lazy<TypeReference> _openEventHandlerT;

        public ModuleImporter(ModuleDefinition moduleDef)
        {
            _moduleDef = moduleDef;
            _compilerGeneratedAttribute = new Lazy<CustomAttribute>(LoadCompilerGeneratedAttribute);
            _openActionTCtor = new Lazy<MethodReference>(LoadOpenActionTConstructor);
            _openEventHandlerT = new Lazy<TypeReference>(LoadOpenEventHandlerT);
        }

        // CompilerGeneratedAttribute
        public CustomAttribute CompilerGeneratedAttribute { get { return _compilerGeneratedAttribute.Value; } }

        // Action<T>.ctor()
        public MethodReference ActionOpenCtor { get { return _openActionTCtor.Value; } }

        // EventHandler<T>
        public TypeReference EventHandlerT { get { return _openEventHandlerT.Value; } }

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
        
        private static string SysObjectName = typeof(System.Object).FullName;
        private static string IntPtrName = typeof(System.IntPtr).FullName;
    }
}
