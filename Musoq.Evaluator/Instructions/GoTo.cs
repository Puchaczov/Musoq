namespace Musoq.Evaluator.Instructions
{
    public class GoTo<T> : ByteCodeInstruction
    {
        private readonly int _value;

        public GoTo(int value)
        {
            _value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine[Register.Ip] = _value;
        }

        public override string DebugInfo()
        {
            return $"GO TO {_value}";
        }
    }
}