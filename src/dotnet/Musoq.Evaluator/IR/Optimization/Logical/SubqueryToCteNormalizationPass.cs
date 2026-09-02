using System.Linq;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed class SubqueryToCteNormalizationPass : IPreLogicalNormalizationPass
{
    public string Name => "SubqueryToCteNormalization";

    public OptimizationResult<RootNode> Optimize(RootNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);

        // Validate the strategy before invoking the rewriting visitor. A
        // residual-only correlated predicate has no bounded lowering path; if
        // it reaches the rewriter first, it can be transformed into an
        // invalid shape and fail later as an internal compiler error.
        var preflightAnalysis = SubqueryCorrelationAnalyzer.Analyze(plan);
        var preflightRequests = CorrelatedSubqueryRewriteRequestBuilder.Build(plan, preflightAnalysis);
        var unsupportedInFallback = preflightRequests.FirstOrDefault(static request =>
            !request.IsDirectFilter &&
            request.Node is InQueryNode &&
            !request.Correlation.HasEqualityKeys);
        if (unsupportedInFallback != null)
            throw SubqueryDiagnosticFactory.InvalidSubquery(
                "correlated subquery strategy",
                "A correlated IN subquery used as a value expression requires an equality correlation key. " +
                "Move the predicate to WHERE, HAVING, or QUALIFY, add an equality correlation, or use an explicit APPLY.",
                unsupportedInFallback.Node);

        var unsupportedQuantifiedFallback = preflightRequests.FirstOrDefault(IsUnsupportedQuantifiedFallback);
        if (unsupportedQuantifiedFallback != null)
            throw SubqueryDiagnosticFactory.InvalidSubquery(
                "correlated quantified subquery strategy",
                "This non-equality quantified predicate reuses its equality correlation key in a value expression " +
                "and cannot be lowered safely. Correlate on a separate equality key, move the predicate to a filter " +
                "clause, or use an explicit APPLY.",
                unsupportedQuantifiedFallback.Node);

        var before = plan.ToString();
        var subqueryRewriter = new SubqueryToCteRewriteVisitor();
        var subqueryTraverser = new SubqueryToCteRewriteTraverseVisitor(subqueryRewriter);
        plan.Accept(subqueryTraverser);

        var rewriteRequests = subqueryRewriter.RewriteRequests;
        var decisions = CorrelatedSubqueryStrategyPlanner.Plan(rewriteRequests);
        context.AnalysisFacts.Set(CorrelatedSubqueryPlanningFacts.RewriteRequests, rewriteRequests);
        context.AnalysisFacts.Set(CorrelatedSubqueryPlanningFacts.Decisions, decisions);

        var normalized = subqueryTraverser.Root;
        var changed = !string.Equals(before, normalized.ToString(), StringComparison.Ordinal);

        return changed
            ? OptimizationResult<RootNode>.Changed(
                normalized,
                CreateChangedReason(normalized))
            : OptimizationResult<RootNode>.NoChange(
                normalized,
                "No supported subquery forms required pre-logical normalization.");
    }

    private static bool IsUnsupportedQuantifiedFallback(CorrelatedSubqueryRewriteRequest request)
    {
        if (request.IsDirectFilter ||
            request.Node is not ExistsQueryNode { Subquery: QueryNode query } ||
            !IsGeneratedQuantifiedSubquery(query))
            return false;

        return query.Where?.Expression != null &&
               ContainsSameKeyNonEqualityComparison(query.Where.Expression, request);
    }

    private static bool IsGeneratedQuantifiedSubquery(QueryNode query)
    {
        var fields = query.Select.Fields;
        return fields.Length == 1 &&
               string.Equals(fields[0].FieldName, "_quantified_key", StringComparison.Ordinal) &&
               fields[0].Expression is IntegerNode;
    }

    private static bool ContainsSameKeyNonEqualityComparison(Node node, CorrelatedSubqueryRewriteRequest request)
    {
        if (node is GreaterNode or GreaterOrEqualNode or LessNode or LessOrEqualNode or DiffNode)
        {
            var columns = SubqueryCorrelationUtilities.CollectAccessColumns(node);
            foreach (var key in request.Correlation.EqualityKeys)
            {
                var localColumn = columns.Any(column =>
                    request.Correlation.LocalAliases.Contains(column.Alias) &&
                    string.Equals(column.Name, key.LocalColumn, StringComparison.OrdinalIgnoreCase));
                var outerColumn = columns.Any(column =>
                    request.Correlation.OuterAliases.Contains(column.Alias) &&
                    string.Equals(column.Name, key.OuterColumn, StringComparison.OrdinalIgnoreCase));
                if (localColumn && outerColumn)
                    return true;
            }
        }

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
            if (ContainsSameKeyNonEqualityComparison(child, request))
                return true;

        return false;
    }

    private static string CreateChangedReason(RootNode normalized)
    {
        var facts = LogicalSubqueryOwnershipFactCollector.Collect(normalized);
        if (facts.Count == 0)
            return "Converted supported subquery forms into CTE/join-compatible pre-logical shapes.";

        var summary = string.Join(
            ", ",
            facts
                .GroupBy(static fact => fact.Kind)
                .OrderBy(static group => group.Key)
                .Select(static group => $"{group.Key}={group.Count()}"));

        return $"Converted supported subquery forms into CTE/join-compatible pre-logical shapes ({summary}).";
    }
}
