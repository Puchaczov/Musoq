namespace Musoq.Evaluator.Instructions
{
    public class LoadNumberToRegister<T> : ByteCodeInstruction
    {
        private readonly Register register;
        private readonly long value;

        public LoadNumberToRegister(long value, Register register)
        {
            this.value = value;
            this.register = register;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[register] = value;
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD REG {register}, {value}";
        }
    }
}