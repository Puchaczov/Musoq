using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Evaluator.IR.SourcePlanning;

namespace Musoq.Evaluator.IR.Physical.SourcePlanning;

internal static class SourcePredicatePhysicalRewriter
{
    public static PhysicalNode Rewrite(
        PhysicalNode physicalPlan,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        if (sourcePlanResults == null || sourcePlanResults.Count == 0 ||
            sourcePredicatePlansBySourceId == null || sourcePredicatePlansBySourceId.Count == 0)
        {
            return physicalPlan;
        }

        var acceptedPredicates = CreateAcceptedPredicates(sourcePlanResults, sourcePredicatePlansBySourceId);
        return acceptedPredicates.Count == 0
            ? physicalPlan
            : RewriteNode(physicalPlan, acceptedPredicates);
    }

    private static PhysicalNode RewriteNode(
        PhysicalNode node,
        IReadOnlyDictionary<string, IReadOnlyList<IrExpression>> acceptedPredicates)
    {
        return node switch
        {
            PhysicalFilterNode filter => RewriteFilter(filter, acceptedPredicates),
            _ => PhysicalPlanRewriter.RewriteChildren(
                node,
                child => RewriteNode(child, acceptedPredicates))
        };
    }

    private static PhysicalNode RewriteFilter(
        PhysicalFilterNode filter,
        IReadOnlyDictionary<string, IReadOnlyList<IrExpression>> acceptedPredicates)
    {
        var input = RewriteNode(filter.Input, acceptedPredicates);
        if (!PhysicalPlanRewriter.TryResolveDirectSchemaScan(input, out var scan) ||
            string.IsNullOrWhiteSpace(scan.SourceContextId) ||
            !acceptedPredicates.TryGetValue(scan.SourceContextId, out var accepted))
        {
            return ReferenceEquals(input, filter.Input)
                ? filter
                : new PhysicalFilterNode(filter.Predicate, input);
        }

        var rewrittenPredicate = SourcePredicateConjunctMatcher.RemoveAcceptedConjuncts(filter.Predicate, accepted);
        if (rewrittenPredicate == null)
            return input;

        if (ReferenceEquals(input, filter.Input) && ReferenceEquals(rewrittenPredicate, filter.Predicate))
            return filter;

        return new PhysicalFilterNode(rewrittenPredicate, input);
    }

    private static Dictionary<string, IReadOnlyList<IrExpression>> CreateAcceptedPredicates(
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        var result = new Dictionary<string, IReadOnlyList<IrExpression>>(StringComparer.Ordinal);

        foreach (var entry in sourcePlanResults)
        {
            if (entry.Value.AcceptedPredicate == null ||
                !sourcePredicatePlansBySourceId.TryGetValue(entry.Key, out var sourcePredicatePlan))
            {
                continue;
            }

            var acceptedPredicates = SourcePredicateConjunctMatcher.MatchAcceptedConjuncts(
                entry.Value.AcceptedPredicate,
                sourcePredicatePlan,
                allowWholePredicateMatch: entry.Value.ResidualPredicate == null);

            if (acceptedPredicates.Count > 0)
                result[entry.Key] = acceptedPredicates;
        }

        return result;
    }
}
