using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static SubqueryCorrelationAnalysis AnalyzeSubqueries(QueryNode query)
    {
        return SubqueryCorrelationAnalyzer.Analyze(query);
    }

    private static (Node? RemainingExpression, List<SubqueryInfo> Subqueries) AttachCorrelation(
        (Node? RemainingExpression, List<SubqueryInfo> Subqueries) extracted,
        SubqueryCorrelationAnalysis analysis)
    {
        ValidateSubqueryAnalysis(analysis);

        return (extracted.RemainingExpression, extracted.Subqueries
            .Select(info => info with { Correlation = FindCorrelation(info.PredicateNode, analysis) })
            .ToList());
    }

    private static void ValidateSubqueryAnalysis(SubqueryCorrelationAnalysis analysis)
    {
        if (!analysis.HasIllegalOuterConsumingCteReferences)
            return;

        throw SubqueryDiagnosticFactory.CteDefinitionConsumesOuterAlias(analysis);
    }

    private static CorrelatedSubqueryRewrite RewriteCorrelatedSubqueryIfNeeded(
        QueryNode query,
        SubqueryInfo subqueryInfo,
        string cteName)
    {
        var correlation = subqueryInfo.Correlation;
        if (correlation is not { IsCorrelated: true })
            return new CorrelatedSubqueryRewrite(query, null);

        if (query.Where == null)
            ThrowUnsupportedCorrelation(subqueryInfo);

        var conjuncts = SplitConjuncts(query.Where.Expression);
        var correlated = conjuncts
            .Where(predicate => ReferencesAnyAlias(predicate, correlation.CorrelatedAliases))
            .ToArray();
        if (correlated.Length == 0)
            ThrowUnsupportedCorrelation(subqueryInfo);

        var local = conjuncts
            .Where(predicate => !correlated.Contains(predicate))
            .ToArray();
        var projections = CollectCorrelationProjections(correlated, correlation.LocalAliases, cteName);
        var rewrittenQuery = AddCorrelationProjectionFields(
            query,
            projections,
            CombineConjuncts(local));
        var joinPredicate = RewriteCorrelatedPredicatesForJoin(
            correlated,
            projections,
            correlation.LocalAliases,
            cteName);

        return new CorrelatedSubqueryRewrite(rewrittenQuery, joinPredicate);
    }

    private static SubqueryCorrelationInfo? FindCorrelation(
        Node node,
        SubqueryCorrelationAnalysis analysis)
    {
        return analysis.Subqueries.FirstOrDefault(info => ReferenceEquals(info.Node, node));
    }

    private static QueryNode AddCorrelationProjectionFields(
        QueryNode query,
        CorrelationProjection[] projections,
        Node? localWhere)
    {
        if (projections.Length == 0 && ReferenceEquals(localWhere, query.Where?.Expression))
            return query;

        var fields = new FieldNode[query.Select.Fields.Length + projections.Length];
        Array.Copy(query.Select.Fields, fields, query.Select.Fields.Length);

        for (var i = 0; i < projections.Length; i++)
        {
            var projection = projections[i];
            fields[query.Select.Fields.Length + i] = new FieldNode(
                new AccessColumnNode(
                    projection.ColumnName,
                    projection.Alias,
                    projection.ReturnType,
                    projection.Span,
                    projection.IntendedTypeName),
                query.Select.Fields.Length + i,
                projection.CteColumnName);
        }

        return new QueryNode(
            new SelectNode(fields, query.Select.IsDistinct),
            query.From,
            localWhere != null ? new WhereNode(localWhere) : null,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedCorrelation(SubqueryInfo info)
    {
        var predicateName = info.IsExists ? "EXISTS" : "IN";
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            $"correlated {predicateName} rewrite",
            $"Correlated {predicateName} subqueries must place outer references in the subquery WHERE clause.",
            info.PredicateNode);
    }

    private sealed record CorrelatedSubqueryRewrite(QueryNode Query, Node? JoinPredicate);
}
