using System.Collections.Generic;

namespace FQL.Evaluator.Instructions
{
    public class JmpState : ByteCodeInstruction
    {
        private readonly bool _expectedState;
        private readonly string _label;
        private readonly IDictionary<string, int> _labels;

        public JmpState(IDictionary<string, int> labels, string label, bool expectedState)
        {
            _labels = labels;
            _label = label;
            _expectedState = expectedState;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            if (virtualMachine.Current.BooleanStack.Pop() == _expectedState)
                virtualMachine[Register.Ip] = _labels[_label];
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