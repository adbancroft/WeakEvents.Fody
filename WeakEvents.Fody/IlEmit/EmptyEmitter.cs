using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    // Empty emitter. Will usually be the root of an emitter chain.
    class EmptyEmitter : IlEmitter
    {
        private MethodDefinition _method;

        public EmptyEmitter(IlEmitter template)
            : this(template.Method)
        {
        }
        public EmptyEmitter(MethodDefinition method)
        {
            _method = method;
        }

        public IEnumerable<Instruction> Emit()
        {
            return Enumerable.Empty<Instruction>();
        }

        public MethodDefinition Method
        {
            get { return _method; }
        }
    }
}
