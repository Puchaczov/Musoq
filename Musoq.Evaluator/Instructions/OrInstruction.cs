namespace Musoq.Evaluator.Instructions
{
    public class OrInstruction<T> : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.BooleanStack;
            stack.Push(stack.Pop() || stack.Pop());

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "OR";
        }
    }
}