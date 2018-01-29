using System.Collections.Generic;

namespace FQL.Evaluator.Instructions
{
    public class Jmp : ByteCodeInstruction
    {
        private readonly string _label;
        private readonly IDictionary<string, int> _labels;

        public Jmp(IDictionary<string, int> labels, string label)
        {
            _labels = labels;
            _label = label;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[Register.Ip] = _labels[_label];
        }

        public override string DebugInfo()
        {
            return $"JMP {_label}";
        }
    }
}