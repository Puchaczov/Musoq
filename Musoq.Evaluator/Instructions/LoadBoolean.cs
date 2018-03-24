namespace Musoq.Evaluator.Instructions
{
    public class LoadBoolean : Instruction
    {
        private readonly bool _value;

        public LoadBoolean(bool value)
        {
            _value = value;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.BooleanStack.Push(_value);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"LD {_value}";
        }
    }
}