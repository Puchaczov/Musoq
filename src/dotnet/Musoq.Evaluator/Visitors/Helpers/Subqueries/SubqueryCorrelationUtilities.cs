using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class SubqueryCorrelationUtilities
{
    public static Node[] SplitConjuncts(Node expression)
    {
        if (expression is not AndNode and)
            return [expression];

        return [..SplitConjuncts(and.Left), ..SplitConjuncts(and.Right)];
    }

    public static Node? CombineConjuncts(IReadOnlyList<Node> expressions)
    {
        if (expressions.Count == 0)
            return null;

        var result = expressions[0];
        for (var i = 1; i < expressions.Count; i++)
            result = new AndNode(result, expressions[i]);

        return result;
    }

    public static bool ReferencesAnyAlias(Node expression, IReadOnlySet<string> aliases)
    {
        return CollectAccessColumns(expression).Any(column => aliases.Contains(column.Alias));
    }

    public static bool IsSingleRangeCorrelation(Node expression) =>
        expression is GreaterNode or GreaterOrEqualNode or LessNode or LessOrEqualNode;

    public static bool IsIndexedRangeCorrelation(Node expression)
    {
        var predicates = SplitConjuncts(expression);
        return predicates.Count(IsSingleRangeCorrelation) == 1 &&
               predicates.All(static predicate => predicate is EqualityNode or IsNullNode || IsSingleRangeCorrelation(predicate));
    }

    public static AccessColumnNode[] CollectAccessColumns(Node expression)
    {
        var columns = new List<AccessColumnNode>();
        CollectAccessColumns(expression, columns);
        return columns.ToArray();
    }

    public static CorrelationProjection[] CollectCorrelationProjections(
        IReadOnlyList<Node> predicates,
        IReadOnlySet<string> localAliases,
        string cteName)
    {
        var projections = new List<CorrelationProjection>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in predicates.SelectMany(CollectAccessColumns))
        {
            if (!TryResolveLocalColumnAlias(column, localAliases, out var alias))
                continue;

            var key = CreateCorrelationColumnKey(alias, column.Name);
            if (!seen.Add(key))
                continue;

            projections.Add(new CorrelationProjection(
                alias,
                column.Name,
                GeneratedSubqueryContract.CreateCorrelationColumnName(cteName, projections.Count),
                column.ReturnType,
                column.Span,
                column.IntendedTypeName));
        }

        return projections.ToArray();
    }

    public static Node RewriteCorrelatedPredicatesForJoin(
        IReadOnlyList<Node> predicates,
        IReadOnlyList<CorrelationProjection> projections,
        IReadOnlySet<string> localAliases,
        string cteName)
    {
        var rewriter = new CorrelationPredicateJoinRewriter(projections, localAliases, cteName);
        return CombineConjuncts(predicates.Select(rewriter.Rewrite).ToArray()) ??
               throw new InvalidOperationException("Correlated predicate join rewrite requires at least one predicate.");
    }

    private static void CollectAccessColumns(Node node, List<AccessColumnNode> columns)
    {
        switch (node)
        {
            case null:
                return;
            case AccessColumnNode accessColumn:
                columns.Add(new AccessColumnNode(
                    accessColumn.Name,
                    accessColumn.Alias,
                    accessColumn.ReturnType ?? typeof(void),
                    accessColumn.Span,
                    accessColumn.IntendedTypeName));
                return;
            case DotNode { Root: IdentifierNode alias, Expression: IdentifierNode column }:
                columns.Add(new AccessColumnNode(column.Name, alias.Name, node.ReturnType ?? typeof(void), node.Span));
                return;
            case DotNode dot:
                CollectAccessColumns(dot.Root, columns);
                CollectAccessColumns(dot.Expression, columns);
                return;
            case BinaryNode binary:
                CollectAccessColumns(binary.Left, columns);
                CollectAccessColumns(binary.Right, columns);
                return;
            case UnaryNode unary:
                CollectAccessColumns(unary.Expression, columns);
                return;
            case IsNullNode isNull:
                CollectAccessColumns(isNull.Expression, columns);
                return;
            case AccessMethodNode method:
                foreach (var argument in method.Arguments.Args)
                    CollectAccessColumns(argument, columns);
                return;
        }
    }

    private static bool TryResolveLocalColumnAlias(
        AccessColumnNode column,
        IReadOnlySet<string> localAliases,
        [NotNullWhen(true)] out string? alias)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias) && localAliases.Contains(column.Alias))
        {
            alias = column.Alias;
            return true;
        }

        if (string.IsNullOrWhiteSpace(column.Alias) && localAliases.Count == 1)
        {
            alias = localAliases.Single();
            return true;
        }

        alias = null;
        return false;
    }

    private static string CreateCorrelationColumnKey(string alias, string columnName) => $"{alias}.{columnName}";

    private sealed class CorrelationPredicateJoinRewriter(
        IReadOnlyList<CorrelationProjection> projections,
        IReadOnlySet<string> localAliases,
        string cteName)
        : CloneQueryVisitor
    {
        private readonly Dictionary<string, CorrelationProjection> _projections = projections.ToDictionary(
            projection => CreateCorrelationColumnKey(projection.Alias, projection.ColumnName),
            StringComparer.OrdinalIgnoreCase);

        public Node Rewrite(Node node)
        {
            node.Accept(new CloneTraverseVisitor(this));
            return Nodes.Pop();
        }

        public override void Visit(AccessColumnNode node)
        {
            if (TryResolveLocalColumnAlias(node, localAliases, out var alias) &&
                _projections.TryGetValue(CreateCorrelationColumnKey(alias, node.Name), out var projection))
            {
                Nodes.Push(new AccessColumnNode(
                    projection.CteColumnName,
                    cteName,
                    projection.ReturnType,
                    node.Span,
                    node.IntendedTypeName));
                return;
            }

            base.Visit(node);
        }

        public override void Visit(DotNode node)
        {
            if (node is { Root: IdentifierNode alias, Expression: IdentifierNode column } &&
                _projections.TryGetValue(CreateCorrelationColumnKey(alias.Name, column.Name), out var projection))
            {
                Nodes.Pop();
                Nodes.Pop();
                Nodes.Push(new AccessColumnNode(
                    projection.CteColumnName,
                    cteName,
                    projection.ReturnType,
                    node.Span));
                return;
            }

            base.Visit(node);
        }
    }
}
