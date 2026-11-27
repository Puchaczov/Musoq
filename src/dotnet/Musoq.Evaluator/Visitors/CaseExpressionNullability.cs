using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class CaseExpressionNullability
{
    internal static bool HasNullableResult(
        IReadOnlyList<(Node When, Node Then)> branches,
        Node otherwise,
        Func<AccessColumnNode, bool> isStableColumn
    )
    {
        foreach (var branch in branches)
            if (IsNullable(branch.Then) && !IsPresentUnder(branch.Then, branch.When, true, isStableColumn))
                return true;

        return IsNullable(otherwise)
            && !branches.Any(branch => IsPresentUnder(otherwise, branch.When, false, isStableColumn));
    }

    private static bool IsNullable(Node value) =>
        SqlPredicateNullability.ResultType(value) is { } type && Nullable.GetUnderlyingType(type) != null;

    private static bool IsPresentUnder(
        Node value,
        Node condition,
        bool conditionIsTrue,
        Func<AccessColumnNode, bool> isStableColumn
    )
    {
        if (
            Unwrap(condition) is not IsNullNode check
            || check.IsNegated != conditionIsTrue
            || Column(Unwrap(value)) is not { } column
            || Column(check.Expression) is not { } checkedColumn
        )
            return false;

        return string.Equals(column.Alias, checkedColumn.Alias, StringComparison.OrdinalIgnoreCase)
            && string.Equals(column.Name, checkedColumn.Name, StringComparison.OrdinalIgnoreCase)
            && isStableColumn(column);
    }

    private static AccessColumnNode? Column(Node expression) =>
        expression switch
        {
            AccessColumnNode column => column,
            DotNode { Root: IdentifierNode root, Expression: AccessColumnNode column }
                when string.Equals(root.Name, column.Alias, StringComparison.OrdinalIgnoreCase) => column,
            _ => null,
        };

    private static Node Unwrap(Node expression) =>
        expression is WhenNode or ThenNode or ElseNode ? ((UnaryNode)expression).Expression : expression;
}
