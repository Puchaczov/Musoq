namespace Musoq.Evaluator.Instructions.Arythmetic
{
    public class NegateBooleanInstruction : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var current = virtualMachine.Current;
            var stack = virtualMachine.Current.BooleanStack;
            var a = stack.Pop();
            current.BooleanStack.Push(!a);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "NOT";
        }
    }
}