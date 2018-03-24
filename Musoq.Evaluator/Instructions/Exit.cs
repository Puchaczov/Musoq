namespace Musoq.Evaluator.Instructions
{
    public class Exit : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[Register.Sop] = (int) SpecialOperationRegister.Exit;
        }

        public override string DebugInfo()
        {
            return "EXIT";
        }
    }
}