namespace Musoq.Evaluator.Instructions.Converts
{
    public class ConvertToDecimal : ByteCodeInstruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.NumericsStack.Push(virtualMachine.Current.LongsStack.Pop());
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "CONV DEC, LNG";
        }
    }
}