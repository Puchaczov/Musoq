using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static DerivedCorrelationRewrite RewriteCorrelatedDerivedBody(
        Node body,
        SubqueryCorrelationInfo correlation,
        string rightAlias,
        DerivedTableFromNode derived)
    {
        switch (body)
        {
            case QueryNode query:
            {
                var rewritten = RewriteCorrelatedDerivedTable(query, correlation, rightAlias, derived);
                return new DerivedCorrelationRewrite(rewritten.Query, rewritten.JoinPredicate, rewritten.CorrelationKey);
            }

            case SingleSetNode singleSet:
            {
                var rewritten = RewriteCorrelatedDerivedTable(singleSet.Query, correlation, rightAlias, derived);
                return new DerivedCorrelationRewrite(new SingleSetNode(rewritten.Query), rewritten.JoinPredicate, rewritten.CorrelationKey);
            }

            case SetOperatorNode setOperator:
                return RewriteCorrelatedDerivedSetOperator(setOperator, correlation, rightAlias, derived);

            default:
                throw CreateUnsupportedDerivedCorrelation(derived,
                    "Correlated APPLY derived tables over this query shape are not supported yet.");
        }
    }

    private static DerivedCorrelationRewrite RewriteCorrelatedDerivedSetOperator(
        SetOperatorNode setOperator,
        SubqueryCorrelationInfo correlation,
        string rightAlias,
        DerivedTableFromNode derived)
    {
        var left = RewriteCorrelatedDerivedBody(setOperator.Left, correlation, rightAlias, derived);
        var right = RewriteCorrelatedDerivedBody(setOperator.Right, correlation, rightAlias, derived);
        var joinPredicate = RequireCompatibleDerivedSetJoinPredicate(left, right, derived);

        return new DerivedCorrelationRewrite(RecreateSetOperator(setOperator, left.Body, right.Body), joinPredicate, left.CorrelationKey);
    }

    private static Node RequireCompatibleDerivedSetJoinPredicate(DerivedCorrelationRewrite left, DerivedCorrelationRewrite right, DerivedTableFromNode derived)
    {
        if (left.JoinPredicate == null || right.JoinPredicate == null)
            ThrowUnsupportedDerivedCorrelation(derived,
                "Every branch of a correlated APPLY set-operator derived table must expose the same projected correlation key.");

        if (!AreSameDerivedCorrelationKey(left.CorrelationKey, right.CorrelationKey) || !string.Equals(left.JoinPredicate.ToString(), right.JoinPredicate.ToString(), StringComparison.Ordinal))
            ThrowUnsupportedDerivedCorrelation(derived,
                "Every branch of a correlated APPLY set-operator derived table must use the same projected correlation key.");

        return left.JoinPredicate;
    }

    private static bool AreSameDerivedCorrelationKey(string[] left, string[] right) => left.Length == right.Length && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);

    private static DerivedCorrelationQueryRewrite RewriteCorrelatedDerivedTable(
        QueryNode query,
        SubqueryCorrelationInfo correlation,
        string rightAlias,
        DerivedTableFromNode derived)
    {
        if (query.Where == null)
            ThrowUnsupportedDerivedCorrelation(derived,
                "Correlated APPLY derived tables must place outer references in the derived query WHERE clause.");

        var conjuncts = SplitConjuncts(query.Where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();
        if (correlated.Length == 0)
            ThrowUnsupportedDerivedCorrelation(derived,
                "Correlated APPLY derived tables must place outer references in the derived query WHERE clause.");

        var local = conjuncts
            .Where(predicate => !correlated.Contains(predicate))
            .ToArray();
        var projections = CollectVisibleDerivedCorrelationProjections(
            query,
            CollectCorrelationProjections(correlated, correlation.LocalAliases, rightAlias),
            derived);
        var rewrittenQuery = new QueryNode(
            query.Select,
            query.From,
            CombineConjuncts(local) is { } localWhere ? new WhereNode(localWhere) : null,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            rightAlias);

        return new DerivedCorrelationQueryRewrite(rewrittenQuery, joinPredicate, projections.Select(static projection => projection.ColumnName).ToArray());
    }

    private static CorrelationProjection[] CollectVisibleDerivedCorrelationProjections(
        QueryNode query,
        CorrelationProjection[] projections,
        DerivedTableFromNode derived)
    {
        var visible = new CorrelationProjection[projections.Length];

        for (var i = 0; i < projections.Length; i++)
        {
            if (!TryFindProjectedCorrelationColumn(query.Select, projections[i], out var columnName))
                ThrowUnsupportedDerivedCorrelation(derived,
                    $"Correlated APPLY derived table must project local correlation column '{projections[i].Alias}.{projections[i].ColumnName}'.");

            visible[i] = projections[i] with { CteColumnName = columnName };
        }

        return visible;
    }

    private static bool TryFindProjectedCorrelationColumn(
        SelectNode select,
        CorrelationProjection projection,
        [NotNullWhen(true)] out string? columnName)
    {
        foreach (var field in select.Fields)
        {
            if (field.Expression is AllColumnsNode star &&
                (string.IsNullOrWhiteSpace(star.Alias) ||
                 string.Equals(star.Alias, projection.Alias, StringComparison.OrdinalIgnoreCase)) &&
                star.ExcludeColumns?.Contains(projection.ColumnName, StringComparer.OrdinalIgnoreCase) != true)
            {
                columnName = projection.ColumnName;
                return true;
            }

            if (!ReferencesProjectedColumn(field.Expression, projection))
                continue;

            columnName = GetSubqueryOutputColumnName(field);
            return true;
        }

        columnName = null;
        return false;
    }

    private static bool ReferencesProjectedColumn(Node expression, CorrelationProjection projection)
    {
        return expression switch
        {
            AccessColumnNode access => string.Equals(access.Name, projection.ColumnName, StringComparison.OrdinalIgnoreCase) &&
                                       (string.IsNullOrWhiteSpace(access.Alias) ||
                                        string.Equals(access.Alias, projection.Alias, StringComparison.OrdinalIgnoreCase)),
            DotNode { Root: IdentifierNode alias, Expression: IdentifierNode column } =>
                string.Equals(alias.Name, projection.Alias, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(column.Name, projection.ColumnName, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
