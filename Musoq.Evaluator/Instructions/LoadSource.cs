using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Instructions
{
    public class LoadSource : Instruction
    {
        private readonly RowSource _source;

        public LoadSource(RowSource rowSource)
        {
            _source = rowSource;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = _source.Rows;
            virtualMachine.Current.SourceStack.Push(source.GetEnumerator());
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "LD SOURCE";
        }
    }
}