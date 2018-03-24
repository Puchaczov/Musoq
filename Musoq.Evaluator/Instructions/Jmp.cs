using System.Collections.Generic;

namespace Musoq.Evaluator.Instructions
{
    public class Jmp : Instruction
    {
        private readonly string _label;
        private readonly IDictionary<string, Label> _labels;

        public Jmp(IDictionary<string, Label> labels, string label)
        {
            _labels = labels;
            _label = label;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[Register.Ip] = _labels[_label].StartIndex;
        }

        public override string DebugInfo()
        {
            return $"JMP {_label}";
        }
    }
}