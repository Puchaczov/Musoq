namespace Musoq.Evaluator.Instructions
{
    public class SkipState<T> : Skip<T>
    {
        private readonly bool _expectedState;

        public SkipState(bool expectedState, int value) : base(value)
        {
            _expectedState = expectedState;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            if (virtualMachine.Current.BooleanStack.Pop() == _expectedState)
                virtualMachine[Register.Ip] += Value;
            else
                virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"SKIP BY {Value} WHEN {_expectedState}".ToUpper();
        }
    }
}