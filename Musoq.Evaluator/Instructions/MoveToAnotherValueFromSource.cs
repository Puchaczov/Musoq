namespace Musoq.Evaluator.Instructions
{
    public class MoveToAnotherValueFromSource : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var source = virtualMachine.Current.SourceStack.Peek();

            if (!source.MoveNext())
            {
                virtualMachine.Current.BooleanStack.Push(false);
                virtualMachine[Register.Ip] += 1;
                return;
            }

            virtualMachine.Current.BooleanStack.Push(true);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "NEXT VALUE FROM SOURCE";
        }
    }
}