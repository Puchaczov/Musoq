using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SchemaLogicalExpression
{
    internal static string Emit(BinaryNode expression, Func<Node, string> emit, bool nullable)
    {
        var left = emit(expression.Left);
        var right = emit(expression.Right);
        var isAnd = expression is AndNode;
        if (!nullable)
            return $"({left} {(isAnd ? "&&" : "||")} {right})";
        var decisive = isAnd ? "false" : "true";
        var other = isAnd ? "true" : "false";
        var operation = isAnd ? "&" : "|";
        return $"(((bool?)({left})) switch {{ {decisive} => (bool?){decisive}, {other} => (bool?)({right}), _ => ((bool?)({right}) {operation} (bool?)null) }})";
    }
}
