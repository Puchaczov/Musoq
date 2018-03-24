namespace Musoq.Evaluator.Instructions
{
    public class LoadLong : Instruction
    {
        private readonly long _value;

        public LoadLong(long value)
        {
            _value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.LongsStack.Push(_value);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD LNG {_value}";
        }
    }
}