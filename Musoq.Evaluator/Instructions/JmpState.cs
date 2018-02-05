using System.Collections.Generic;

namespace Musoq.Evaluator.Instructions
{
    public class JmpState : ByteCodeInstruction
    {
        private readonly bool _expectedState;
        private readonly string _label;
        private readonly IDictionary<string, Label> _labels;

        public JmpState(IDictionary<string, Label> labels, string label, bool expectedState)
        {
            _labels = labels;
            _label = label;
            _expectedState = expectedState;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            if (virtualMachine.Current.BooleanStack.Pop() == _expectedState)
                virtualMachine[Register.Ip] = _labels[_label].StartIndex;
            else
                virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            if (_expectedState)
                return $"JMPT {_label}";
            return $"JMPF {_label}";
        }
    }
}