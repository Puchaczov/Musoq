using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Helpers;

internal static class CoalesceOperandFacts
{
    internal static bool IsNullLiteral(Node node) => node is NullNode;

    internal static bool IsRequiredValue(Type? type) =>
        type is { IsValueType: true } and not NullNode.NullType && Nullable.GetUnderlyingType(type) == null;

    internal static Node? KnownResult(CoalesceNode node) =>
        IsNullLiteral(node.Left) ? node.Right
        : IsRequiredValue(node.Left.ReturnType) ? node.Left
        : null;
}
