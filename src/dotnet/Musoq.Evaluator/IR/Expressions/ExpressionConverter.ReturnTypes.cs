using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static Type RequireReturnType(Node node, Type? fallback = null)
    {
        return node.ReturnType ?? fallback ??
            throw new InvalidOperationException($"AST node '{node.GetType().Name}' is missing ReturnType; cannot lower to IR.");
    }
}
