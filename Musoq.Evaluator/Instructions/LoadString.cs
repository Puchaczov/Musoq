namespace Musoq.Evaluator.Instructions
{
    public class LoadString : Instruction
    {
        private readonly string _value;

        public LoadString(string value)
        {
            _value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.StringsStack.Push(_value);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD STR {_value}";
        }
    }
}