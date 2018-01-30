namespace FQL.Evaluator.Instructions
{
    public class AndInstruction<T> : ByteCodeInstruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.BooleanStack;
            stack.Push(stack.Pop() && stack.Pop());

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "AND";
        }
    }
}