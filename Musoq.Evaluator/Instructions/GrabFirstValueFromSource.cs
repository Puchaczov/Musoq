using System.Collections.Generic;

namespace Musoq.Evaluator.Instructions
{
    public class GrabFirstValueFromSource : ByteCodeInstruction
    {
        private readonly string _label;
        private readonly IDictionary<string, Label> _labels;

        public GrabFirstValueFromSource(IDictionary<string, Label> labels, string label)
        {
            _labels = labels;
            _label = label;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();

            if (!source.MoveNext())
                virtualMachine[Register.Ip] = _labels[_label].StartIndex;
            else
                virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "GRAB FROM SOURCE";
        }
    }
}