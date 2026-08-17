using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal static class CorrelatedSubqueryStrategyPlanner
{
    public static IReadOnlyList<CorrelatedSubqueryDecision> Plan(
        IReadOnlyList<CorrelatedSubqueryRewriteRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.Select(Decide).ToArray();
    }

    public static CorrelatedSubqueryDecision Decide(CorrelatedSubqueryRewriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Correlation.HasEqualityKeys)
        {
            return new CorrelatedSubqueryDecision(
                request,
                CorrelatedSubqueryStrategyKind.Unsupported,
                "No equality correlation key is available for a bounded hash strategy; per-row APPLY is not selected implicitly.");
        }

        if (request.IsScalar)
        {
            var hasSlicing = HasContext(request, SubqueryCardinalityContextKind.Skip) ||
                             HasContext(request, SubqueryCardinalityContextKind.Take);
            return hasSlicing
                ? new CorrelatedSubqueryDecision(
                    request,
                    CorrelatedSubqueryStrategyKind.PartitionedTopOffset,
                    "Scalar row slicing must be evaluated independently per correlation key.")
                : new CorrelatedSubqueryDecision(
                    request,
                    CorrelatedSubqueryStrategyKind.HashSingleJoin,
                    $"Scalar cardinality is enforced once per correlation key in the {request.EvaluationPhase} phase.");
        }

        if (!request.IsPredicate)
        {
            return new CorrelatedSubqueryDecision(
                request,
                CorrelatedSubqueryStrategyKind.Apply,
                "The correlated source is not a scalar or predicate wrapper and requires an explicit APPLY-capable lowering.");
        }

        if (!request.IsDirectFilter)
        {
            return new CorrelatedSubqueryDecision(
                request,
                CorrelatedSubqueryStrategyKind.HashMarkJoin,
                $"Predicate truth must remain available as a value in the {request.EvaluationPhase} phase.");
        }

        return request.IsNegated
            ? new CorrelatedSubqueryDecision(
                request,
                CorrelatedSubqueryStrategyKind.HashAntiJoin,
                "A directly filtering negated predicate can use one anti lookup per correlation key.")
            : new CorrelatedSubqueryDecision(
                request,
                CorrelatedSubqueryStrategyKind.HashSemiJoin,
                "A directly filtering predicate can use one semi lookup per correlation key.");
    }

    private static bool HasContext(
        CorrelatedSubqueryRewriteRequest request,
        SubqueryCardinalityContextKind kind)
    {
        return request.Correlation.CardinalitySensitiveContexts.Any(context => context.Kind == kind);
    }
}
