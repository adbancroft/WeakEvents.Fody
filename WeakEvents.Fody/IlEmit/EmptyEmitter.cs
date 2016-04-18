using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace WeakEvents.Fody.IlEmit
{
    // Empty emitter. Will usually be the root of an emitter chain.
    internal class EmptyEmitter : ILEmitter
    {
        private readonly MethodDefinition _method;
        private readonly ModuleImporter _importer;

        public EmptyEmitter(ILEmitter template)
            : this(template.Method, template.Importer)
        {
        }

        public EmptyEmitter(MethodDefinition method, ModuleImporter importer)
        {
            _method = method;
            _importer = importer;
        }

        public IEnumerable<Instruction> Emit()
        {
            return Enumerable.Empty<Instruction>();
        }

        public MethodDefinition Method
        {
            get { return _method; }
        }

        public ModuleImporter Importer
        {
            get { return _importer; }
        }
    }
}