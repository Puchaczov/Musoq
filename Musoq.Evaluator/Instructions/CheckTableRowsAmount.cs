using System.Collections.Generic;

namespace Musoq.Evaluator.Instructions
{
    public class CheckTableRowsAmount : ByteCodeInstruction
    {
        private readonly long _value;
        private readonly Dictionary<string, Label> _labels;
        private readonly string _endOfQueryProcessing;
        private int _counter;

        public CheckTableRowsAmount(long value, Dictionary<string, Label> labels, string endOfQueryProcessing)
        {
            _value = value;
            _labels = labels;
            _endOfQueryProcessing = endOfQueryProcessing;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            if (_counter >= _value)
            {
                virtualMachine[Register.Ip] = _labels[_endOfQueryProcessing].StartIndex;
                return;
            }

            _counter += 1;
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"HAS {_value} ROWS";
        }
    }
}