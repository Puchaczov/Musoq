namespace Musoq.Evaluator.Instructions
{
    public class Skip<T> : Instruction
    {
        protected readonly int Value;

        public Skip(int value)
        {
            Value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[Register.Ip] += Value;
        }

        public override string DebugInfo()
        {
            return $"SKIP {Value}";
        }
    }
}