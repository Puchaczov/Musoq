using System;

namespace FQL.Evaluator.Instructions.Arythmetic
{
    public class LessOrEqualStringInstruction : ByteCodeInstruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var current = virtualMachine.Current;
            var stack = virtualMachine.Current.StringsStack;
            var b = stack.Pop();
            var a = stack.Pop();
            var score = string.Compare(a, b, StringComparison.Ordinal);
            current.BooleanStack.Push(score <= 0);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "LESS OR EQUAL STR";
        }
    }
}