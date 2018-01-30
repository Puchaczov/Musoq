namespace Musoq.Evaluator.Instructions
{
    public class PreparePropertyCall<T> : ByteCodeInstruction
    {
        private readonly object _obj;

        public PreparePropertyCall(object obj)
        {
            _obj = obj;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            virtualMachine.Current.ObjectsStack.Push(_obj);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "PREPARE PROPERTY CALL";
        }
    }
}