using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SqlPredicateNullability
{
    // A bound predicate can produce UNKNOWN even when the parser node declares a Boolean operator.
    internal static Type? ResultType(Node node) =>
        node.ReturnType == typeof(bool) && CanReturnUnknown(node) ? typeof(bool?) : node.ReturnType;

    private static bool CanReturnUnknown(Node node) =>
        node switch
        {
            NullNode or InNode => true,
            CollectionInNode collection => CanBeNull(collection.Left)
                || collection.Collection.ReturnType?.GetElementType() is { } elementType
                    && (!elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null),
            UnaryNode unary when unary is NotNode or ThenNode or ElseNode => CanReturnUnknown(unary.Expression),
            BinaryNode logical when logical is AndNode or OrNode => CanReturnUnknown(logical.Left)
                || CanReturnUnknown(logical.Right),
            BinaryNode comparison
                when comparison
                    is EqualityNode
                        or DiffNode
                        or GreaterNode
                        or GreaterOrEqualNode
                        or LessNode
                        or LessOrEqualNode => CanBeNull(comparison.Left) || CanBeNull(comparison.Right),
            BetweenNode between => CanBeNull(between.Expression) || CanBeNull(between.Min) || CanBeNull(between.Max),
            _ => node.ReturnType == typeof(bool?),
        };

    private static bool CanBeNull(Node node) =>
        node.ReturnType is not { } type
        || !type.IsValueType
        || Nullable.GetUnderlyingType(type) != null
        || type == typeof(bool) && CanReturnUnknown(node);
}
