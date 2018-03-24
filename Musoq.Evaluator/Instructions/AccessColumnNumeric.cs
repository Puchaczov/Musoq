using System;

namespace Musoq.Evaluator.Instructions
{
    public class AccessColumnNumeric<T> : Instruction
    {
        private readonly int _order;

        public AccessColumnNumeric(int order)
        {
            _order = order;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();
            var value = ((object[]) source.Current.Context)[_order];

            virtualMachine.Current.ObjectsStack.Push(value);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            throw new NotImplementedException();
        }
    }
}