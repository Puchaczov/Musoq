using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class StringConcatenationTyping
{
    internal static bool IsOperand(Node operand) => operand is NullNode || operand.ReturnType == typeof(string);
    internal static Node Contextualize(Node operand) => operand is NullNode
        ? new NullNode(typeof(string), operand.Span) : operand;
}
