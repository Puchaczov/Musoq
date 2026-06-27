using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static partial class SubqueryCorrelationFactBuilder
{
    public static SubqueryCorrelationFacts Build(
        Node node,
        IReadOnlySet<string> localAliases,
        IReadOnlySet<string> outerAliases,
        IReadOnlySet<string> correlatedAliases)
    {
        var body = GetSubqueryBody(node);
        var equalityKeys = CollectEqualityKeys(body, localAliases, outerAliases);
        var contexts = CollectCardinalityContexts(node, body);

        return new SubqueryCorrelationFacts(
            CopyAliases(localAliases),
            CopyAliases(outerAliases),
            CopyAliases(correlatedAliases),
            equalityKeys,
            ResolveNullSemantics(correlatedAliases, equalityKeys),
            contexts);
    }

    private static Node GetSubqueryBody(Node node)
    {
        return node switch
        {
            InQueryNode inQuery => inQuery.Subquery,
            ExistsQueryNode existsQuery => existsQuery.Subquery,
            ScalarSubqueryNode scalarSubquery => scalarSubquery.Subquery,
            _ => node
        };
    }

    private static SubqueryCorrelationKeyFact[] CollectEqualityKeys(
        Node body,
        IReadOnlySet<string> localAliases,
        IReadOnlySet<string> outerAliases)
    {
        var keys = new List<SubqueryCorrelationKeyFact>();
        CollectEqualityKeys(body, localAliases, outerAliases, keys);
        return keys.ToArray();
    }

    private static void CollectEqualityKeys(
        Node node,
        IReadOnlySet<string> localAliases,
        IReadOnlySet<string> outerAliases,
        List<SubqueryCorrelationKeyFact> keys)
    {
        switch (node)
        {
            case QueryNode { Where: not null } query:
                foreach (var conjunct in SplitConjuncts(query.Where.Expression))
                    if (TryCreateEqualityKey(conjunct, localAliases, outerAliases, out var key))
                        keys.Add(key);
                return;

            case SingleSetNode singleSet:
                CollectEqualityKeys(singleSet.Query, localAliases, outerAliases, keys);
                return;

            case SetOperatorNode setOperator:
                CollectEqualityKeys(setOperator.Left, localAliases, outerAliases, keys);
                CollectEqualityKeys(setOperator.Right, localAliases, outerAliases, keys);
                return;

            case CteExpressionNode cte:
                foreach (var expression in cte.InnerExpression)
                    CollectEqualityKeys(expression.Value, localAliases, outerAliases, keys);
                CollectEqualityKeys(cte.OuterExpression, localAliases, outerAliases, keys);
                return;
        }
    }

    private static bool TryCreateEqualityKey(
        Node predicate,
        IReadOnlySet<string> localAliases,
        IReadOnlySet<string> outerAliases,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SubqueryCorrelationKeyFact? key)
    {
        key = null;
        if (predicate is not EqualityNode equality ||
            !TryGetDirectColumn(equality.Left, out var leftColumn) ||
            !TryGetDirectColumn(equality.Right, out var rightColumn) ||
            !TryCreateKeySide(leftColumn, localAliases, outerAliases, out var leftSide) ||
            !TryCreateKeySide(rightColumn, localAliases, outerAliases, out var rightSide) ||
            leftSide.Kind == rightSide.Kind)
        {
            return false;
        }

        var local = leftSide.Kind == CorrelationKeySideKind.Local ? leftSide : rightSide;
        var outer = leftSide.Kind == CorrelationKeySideKind.Outer ? leftSide : rightSide;
        key = new SubqueryCorrelationKeyFact(
            local.Alias,
            local.ColumnName,
            local.ReturnType,
            local.Span,
            local.IntendedTypeName,
            outer.Alias,
            outer.ColumnName,
            outer.ReturnType,
            outer.Span,
            outer.IntendedTypeName);
        return true;
    }

    private static bool TryGetDirectColumn(
        Node node,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AccessColumnNode? column)
    {
        switch (node)
        {
            case AccessColumnNode accessColumn:
                column = accessColumn;
                return true;

            case DotNode { Root: IdentifierNode alias, Expression: IdentifierNode identifier }:
                column = new AccessColumnNode(identifier.Name, alias.Name, node.ReturnType ?? typeof(void), node.Span);
                return true;

            default:
                column = null;
                return false;
        }
    }

    private static bool TryCreateKeySide(
        AccessColumnNode column,
        IReadOnlySet<string> localAliases,
        IReadOnlySet<string> outerAliases,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CorrelationKeySide? side)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
        {
            if (localAliases.Contains(column.Alias))
            {
                side = CreateSide(CorrelationKeySideKind.Local, column.Alias, column);
                return true;
            }

            if (outerAliases.Contains(column.Alias))
            {
                side = CreateSide(CorrelationKeySideKind.Outer, column.Alias, column);
                return true;
            }
        }

        if (string.IsNullOrWhiteSpace(column.Alias) && localAliases.Count == 1)
        {
            side = CreateSide(CorrelationKeySideKind.Local, localAliases.Single(), column);
            return true;
        }

        side = null;
        return false;
    }

    private static CorrelationKeySide CreateSide(
        CorrelationKeySideKind kind,
        string alias,
        AccessColumnNode column)
    {
        return new CorrelationKeySide(
            kind,
            alias,
            column.Name,
            column.ReturnType ?? typeof(void),
            column.Span,
            column.IntendedTypeName);
    }

    private static SubqueryCorrelationNullSemantics ResolveNullSemantics(
        IReadOnlySet<string> correlatedAliases,
        IReadOnlyList<SubqueryCorrelationKeyFact> equalityKeys)
        => correlatedAliases.Count == 0
            ? SubqueryCorrelationNullSemantics.NotCorrelated
            : equalityKeys.Count > 0
                ? SubqueryCorrelationNullSemantics.EqualityComparison
                : SubqueryCorrelationNullSemantics.ResidualOrUnknown;

    private static HashSet<string> CopyAliases(IEnumerable<string> aliases)
        => new(aliases, StringComparer.OrdinalIgnoreCase);

    private sealed record CorrelationKeySide(
        CorrelationKeySideKind Kind,
        string Alias,
        string ColumnName,
        Type ReturnType,
        Musoq.Parser.TextSpan Span,
        string? IntendedTypeName);

    private enum CorrelationKeySideKind
    {
        Local,
        Outer
    }
}
