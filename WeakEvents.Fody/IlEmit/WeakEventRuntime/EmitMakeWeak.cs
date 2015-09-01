using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.MakeWeak<T>()
    class EmitMakeWeak : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitMakeWeak(IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandler, IlEmitter unsubscribe)
            : base(preceedingCode)
        {
            var openMakeWeak = LoadOpenMakeWeakT();
            _inner = Method.Call(openMakeWeak.MakeMethodClosedGeneric(closedHandlerType.GenericArguments[0]), eventHandler, unsubscribe);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }

        private MethodReference LoadOpenMakeWeakT()
        {
            var moduleDef = Method.Module;
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

        private static string SysEventArgsName = typeof(System.EventArgs).FullName;
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter MakeWeak(this IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandler, IlEmitter unsubscribe)
        {
            return new EmitMakeWeak(preceedingCode, closedHandlerType, eventHandler, unsubscribe);
        }
        public static IlEmitter MakeWeak(this MethodDefinition method, GenericInstanceType closedHandlerType, IlEmitter eventHandler, IlEmitter unsubscribe)
        {
            return MakeWeak(new EmptyEmitter(method), closedHandlerType, eventHandler, unsubscribe);
        }
    }
}
