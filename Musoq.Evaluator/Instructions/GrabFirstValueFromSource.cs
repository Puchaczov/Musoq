using System.Collections.Generic;

namespace Musoq.Evaluator.Instructions
{
    public class GrabFirstValueFromSource : ByteCodeInstruction
    {
        private readonly string _label;
        private readonly IDictionary<string, int> _labels;

        public GrabFirstValueFromSource(IDictionary<string, int> labels, string label)
        {
            _labels = labels;
            _label = label;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();

            if (!source.MoveNext())
                virtualMachine[Register.Ip] = _labels[_label];
            else
                virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "GRAB FROM SOURCE";
        }
    }
}