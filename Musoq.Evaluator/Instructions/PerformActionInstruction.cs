using System;

namespace Musoq.Evaluator.Instructions
{
    public class PerformActionInstruction : Instruction
    {
        private readonly Action<IVirtualMachine> _action;
        private readonly string _desc;

        public PerformActionInstruction(Action<IVirtualMachine> action, string desc)
        {
            _action = action;
            _desc = desc;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            _action(virtualMachine);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return _desc;
        }
    }
}