using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Instructions
{
    public class SkipRows : Instruction
    {
        private readonly long _value;
        private Dictionary<string, Label> _labels;
        private string _endOfQueryProcessing;

        public SkipRows(long value)
        {
            _value = value;
        }

        public SkipRows(long value, Dictionary<string, Label> labels, string endOfQueryProcessing) : this(value)
        {
            _labels = labels;
            _endOfQueryProcessing = endOfQueryProcessing;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();

            long index = 0;
            bool canMoveNext = true;

            while (index < _value && (canMoveNext = source.MoveNext()))
            {
                index += 1;
            }

            if (!canMoveNext)
            {
                virtualMachine[Register.Ip] = _labels[_endOfQueryProcessing].StartIndex;
            }
            else
            {
                virtualMachine[Register.Ip] += 1;
            }
        }

        public override string DebugInfo()
        {
            return $"SKIP {_value}";
        }
    }
}
