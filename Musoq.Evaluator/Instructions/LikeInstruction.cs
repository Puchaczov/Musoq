using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Instructions
{
    public class LikeInstruction : Instruction
    {
        public override void Execute(IVirtualMachine virtualMachine)
        {
            var expression = virtualMachine.Current.StringsStack.Pop();
            var content = virtualMachine.Current.StringsStack.Pop();
            var result =
                new Regex(
                    @"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(expression, ch => @"\" + ch)
                        .Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(content);
            virtualMachine.Current.BooleanStack.Push(result);
            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "LIKE";
        }
    }
}