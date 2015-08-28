using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using WeakEvents.Fody.IlEmit.StandardIl;

namespace WeakEvents.Fody.IlEmit.WeakEventRuntime
{
    // WeakEvents.Runtime.WeakEventHandlerExtensions.FindWeak<T>(eventHandlerDelegate, strongEventHandler)
    class EmitFindWeak : IlEmitterBase
    {
        private readonly IlEmitter _inner;

        public EmitFindWeak(IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandlerDelegate, IlEmitter strongEventHandler)
            : base(preceedingCode)
        {
            var openFindWeak = LoadOpenFindWeakT();
            var findWeakParams = eventHandlerDelegate.Concat(strongEventHandler);
            _inner = Method.Call(openFindWeak.MakeMethodClosedGeneric(closedHandlerType.GenericArguments[0]), findWeakParams);
        }

        public override IEnumerable<Instruction> Emit()
        {
            return EmitPreceeding().Concat(_inner.Emit());
        }

        private MethodReference LoadOpenFindWeakT()
        {
            var moduleDef = Method.Module;
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

        private static string SysEventArgsName = typeof(System.EventArgs).FullName;
        private static string DelegateName = typeof(System.Delegate).FullName;
    }

    static partial class EmitterExtensions
    {
        public static IlEmitter FindWeak(this IlEmitter preceedingCode, GenericInstanceType closedHandlerType, IlEmitter eventHandlerDelegate, IlEmitter strongEventHandler)
        {
            return new EmitFindWeak(preceedingCode, closedHandlerType, eventHandlerDelegate, strongEventHandler);
        }
        public static IlEmitter FindWeak(this MethodDefinition method, GenericInstanceType closedHandlerType, IlEmitter eventHandlerDelegate, IlEmitter strongEventHandler)
        {
            return FindWeak(new EmptyEmitter(method), closedHandlerType, eventHandlerDelegate, strongEventHandler);
        }
    }
}
