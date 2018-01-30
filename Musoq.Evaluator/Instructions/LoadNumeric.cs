namespace FQL.Evaluator.Instructions
{
    public class LoadNumeric : ByteCodeInstruction
    {
        private readonly decimal _value;

        public LoadNumeric(decimal value)
        {
            _value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.NumericsStack.Push(_value);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD LNG {_value}";
        }
    }
}