namespace Musoq.Evaluator.Instructions
{
    public class Pop<T> : Instruction
    {
        public enum StackType
        {
            Boolean,
            Numeric,
            String,
            Object
        }

        private readonly StackType _stackType;

        public Pop(StackType stackType)
        {
            _stackType = stackType;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            switch (_stackType)
            {
                case StackType.Boolean:
                    virtualMachine.Current.BooleanStack.Pop();
                    break;
                case StackType.Numeric:
                    virtualMachine.Current.LongsStack.Pop();
                    break;
                case StackType.String:
                    virtualMachine.Current.StringsStack.Pop();
                    break;
                case StackType.Object:
                    virtualMachine.Current.ObjectsStack.Pop();
                    break;
            }

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"POP STACK";
        }
    }
}