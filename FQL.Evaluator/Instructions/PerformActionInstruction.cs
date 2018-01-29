using System;

namespace FQL.Evaluator.Instructions
{
    public class PerformActionInstruction : ByteCodeInstruction
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