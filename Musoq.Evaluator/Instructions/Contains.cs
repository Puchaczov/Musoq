using System;

namespace Musoq.Evaluator.Instructions
{
    public class Contains<TCompareType> : Instruction
    {
        private readonly Func<StackFrame, TCompareType> _pop;
        private readonly Register _register;

        public Contains(Register register, Func<StackFrame, TCompareType> pop)
        {
            _register = register;
            _pop = pop;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var argsCount = virtualMachine[_register];

            var leftValue = _pop(virtualMachine.Current);

            var containsValue = false;
            var i = 0;
            for (; i < argsCount; ++i)
            {
                if (!_pop(virtualMachine.Current).Equals(leftValue)) continue;

                containsValue = true;
                i += 1;
                break;
            }

            for (; i < argsCount; ++i)
                _pop(virtualMachine.Current);

            virtualMachine.Current.BooleanStack.Push(containsValue);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "CONTAINS";
        }
    }
}