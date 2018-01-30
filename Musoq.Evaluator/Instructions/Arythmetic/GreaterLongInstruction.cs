namespace Musoq.Evaluator.Instructions.Arythmetic
{
    public class GreaterLongInstruction : ByteCodeInstruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var current = virtualMachine.Current;
            var stack = virtualMachine.Current.LongsStack;
            var b = stack.Pop();
            var a = stack.Pop();
            var score = a.CompareTo(b);
            current.BooleanStack.Push(score > 0);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "GREATER LONGS";
        }
    }
}