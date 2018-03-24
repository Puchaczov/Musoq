namespace Musoq.Evaluator.Instructions.Arythmetic
{
    public class EqualStringInstruction : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var stack = virtualMachine.Current.StringsStack;
            virtualMachine.Current.BooleanStack.Push(stack.Pop() == stack.Pop());

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "EQUALS STR";
        }
    }
}