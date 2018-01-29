using System.Collections.Generic;
using FQL.Schema.DataSources;

namespace FQL.Evaluator.Instructions
{
    public class LoadSource : ByteCodeInstruction
    {
        private readonly IEnumerable<IObjectResolver> _source;

        public LoadSource(IEnumerable<IObjectResolver> enumarble)
        {
            _source = enumarble;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.SourceStack.Push(_source.GetEnumerator());
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "LD SOURCE";
        }
    }
}