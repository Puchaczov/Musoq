using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;

namespace Musoq.Evaluator.IR.Physical.SourcePlanning;

internal static class SourcePlanPhysicalRewriter
{
    public static SourcePlanPhysicalRewriteResult Rewrite(
        PhysicalNode physicalPlan,
        IReadOnlyDictionary<string, SourcePlanResult>? sourcePlanResults)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        if (sourcePlanResults == null || sourcePlanResults.Count == 0)
            return new SourcePlanPhysicalRewriteResult(physicalPlan, sourcePlanResults ?? new Dictionary<string, SourcePlanResult>());

        var acceptedOperations = sourcePlanResults.ToDictionary(
            static entry => entry.Key,
            static entry => new AcceptedSourceOperations(
                entry.Value.ExecutionPlan.Identity,
                entry.Value.ExecutionPlan.AcceptedColumns,
                entry.Value.ExecutionPlan.AcceptedPredicate,
                entry.Value.ExecutionPlan.Properties),
            StringComparer.Ordinal);
        var rewrittenPlan = RewriteNode(physicalPlan, sourcePlanResults, acceptedOperations);
        var rewrittenResults = RewriteSourcePlanResults(sourcePlanResults, acceptedOperations);

        return new SourcePlanPhysicalRewriteResult(rewrittenPlan, rewrittenResults);
    }

    private static PhysicalNode RewriteNode(
        PhysicalNode node,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        return node switch
        {
            PhysicalCteNode or PhysicalMultiStatementNode => PhysicalPlanRewriter.RewriteChildren(
                node,
                child => RewriteNode(child, sourcePlanResults, acceptedOperations)),
            PhysicalTopOffsetNode topOffset => RewriteTopOffset(topOffset, sourcePlanResults, acceptedOperations),
            PhysicalTopNNode topN => RewriteTopN(topN, sourcePlanResults, acceptedOperations),
            PhysicalTakeNode take => RewriteTake(take, sourcePlanResults, acceptedOperations),
            PhysicalSkipNode skip => RewriteSkip(skip, sourcePlanResults, acceptedOperations),
            PhysicalSortNode sort => RewriteSort(sort, sourcePlanResults, acceptedOperations),
            _ => node
        };
    }

    private static PhysicalNode RewriteTopOffset(
        PhysicalTopOffsetNode topOffset,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var input = RewriteNode(topOffset.Input, sourcePlanResults, acceptedOperations);

        if (!TryResolveDirectSourcePlan(input, sourcePlanResults, out var scan, out var sourcePlan))
        {
            return ReferenceEquals(input, topOffset.Input)
                ? topOffset
                : new PhysicalTopOffsetNode(topOffset.Skip, topOffset.Take, topOffset.Keys, input);
        }

        var sourceContextId = scan.SourceContextId ??
                              throw new InvalidOperationException("Source planning rewrite requires a source context id.");
        var orderAccepted = CanRemoveOrder(sourcePlan, topOffset.Keys, scan);
        var skipAccepted = orderAccepted && CanRemoveSkip(sourcePlan, topOffset.Skip);
        var takeAccepted = skipAccepted && CanRemoveTake(sourcePlan, topOffset.Take);

        if (!orderAccepted)
            return new PhysicalTopOffsetNode(topOffset.Skip, topOffset.Take, topOffset.Keys, input);

        AcceptOrder(acceptedOperations, sourceContextId, sourcePlan.AcceptedOrderBy);

        if (!skipAccepted)
            return new PhysicalTakeNode(topOffset.Take, new PhysicalSkipNode(topOffset.Skip, input));

        AcceptSkip(acceptedOperations, sourceContextId, sourcePlan.AcceptedSkip);

        if (!takeAccepted)
            return new PhysicalTakeNode(topOffset.Take, input);

        AcceptTake(acceptedOperations, sourceContextId, sourcePlan.AcceptedTake);
        return input;
    }

    private static PhysicalNode RewriteTopN(
        PhysicalTopNNode topN,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var input = RewriteNode(topN.Input, sourcePlanResults, acceptedOperations);

        if (!TryResolveDirectSourcePlan(input, sourcePlanResults, out var scan, out var sourcePlan))
        {
            return ReferenceEquals(input, topN.Input)
                ? topN
                : new PhysicalTopNNode(topN.N, topN.Keys, input);
        }

        var sourceContextId = scan.SourceContextId ??
                              throw new InvalidOperationException("Source planning rewrite requires a source context id.");
        var orderAccepted = CanRemoveOrder(sourcePlan, topN.Keys, scan);
        var takeAccepted = orderAccepted && CanRemoveTake(sourcePlan, topN.N);

        if (!orderAccepted)
            return new PhysicalTopNNode(topN.N, topN.Keys, input);

        AcceptOrder(acceptedOperations, sourceContextId, sourcePlan.AcceptedOrderBy);

        if (!takeAccepted)
            return new PhysicalTakeNode(topN.N, input);

        AcceptTake(acceptedOperations, sourceContextId, sourcePlan.AcceptedTake);
        return input;
    }

    private static PhysicalNode RewriteTake(
        PhysicalTakeNode take,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var input = RewriteNode(take.Input, sourcePlanResults, acceptedOperations);

        if (TryResolveDirectSourcePlan(input, sourcePlanResults, out var scan, out var sourcePlan) &&
            CanRemoveTake(sourcePlan, take.Count))
        {
            AcceptTake(acceptedOperations, RequireSourceContextId(scan), sourcePlan.AcceptedTake);
            return input;
        }

        return ReferenceEquals(input, take.Input)
            ? take
            : new PhysicalTakeNode(take.Count, input);
    }

    private static PhysicalNode RewriteSkip(
        PhysicalSkipNode skip,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var input = RewriteNode(skip.Input, sourcePlanResults, acceptedOperations);

        if (TryResolveDirectSourcePlan(input, sourcePlanResults, out var scan, out var sourcePlan) &&
            CanRemoveSkip(sourcePlan, skip.Count))
        {
            AcceptSkip(acceptedOperations, RequireSourceContextId(scan), sourcePlan.AcceptedSkip);
            return input;
        }

        return ReferenceEquals(input, skip.Input)
            ? skip
            : new PhysicalSkipNode(skip.Count, input);
    }

    private static PhysicalNode RewriteSort(
        PhysicalSortNode sort,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var input = RewriteNode(sort.Input, sourcePlanResults, acceptedOperations);

        if (TryResolveDirectSourcePlan(input, sourcePlanResults, out var scan, out var sourcePlan) &&
            CanRemoveOrder(sourcePlan, sort.Keys, scan))
        {
            AcceptOrder(acceptedOperations, RequireSourceContextId(scan), sourcePlan.AcceptedOrderBy);
            return input;
        }

        return ReferenceEquals(input, sort.Input)
            ? sort
            : new PhysicalSortNode(sort.Keys, input);
    }

    private static bool TryResolveDirectSourcePlan(
        PhysicalNode input,
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        [NotNullWhen(true)] out PhysicalSchemaScanNode? scan,
        [NotNullWhen(true)] out SourcePlanResult? sourcePlan)
    {
        if (PhysicalPlanRewriter.TryResolveDirectSchemaScan(input, out scan) &&
            !string.IsNullOrWhiteSpace(scan.SourceContextId) &&
            sourcePlanResults.TryGetValue(scan.SourceContextId, out sourcePlan))
        {
            return true;
        }

        sourcePlan = null;
        return false;
    }
    private static bool CanRemoveOrder(
        SourcePlanResult sourcePlan,
        OrderField[] keys,
        PhysicalSchemaScanNode scan)
    {
        if (keys.Length == 0)
            return true;

        if (sourcePlan.ResidualOrderBy.Count > 0 ||
            sourcePlan.AcceptedOrderBy.Count != keys.Length)
        {
            return false;
        }

        for (var index = 0; index < keys.Length; index++)
        {
            var accepted = sourcePlan.AcceptedOrderBy[index];
            var key = keys[index];

            if (key.NullOrdering != NullOrdering.Default || key.Expression is not ColumnRef columnRef ||
                !string.Equals(columnRef.Alias, scan.Alias, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(columnRef.ColumnName, accepted.Column.Name, StringComparison.OrdinalIgnoreCase) ||
                key.Descending != (accepted.Direction == OrderDirection.Descending))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanRemoveSkip(SourcePlanResult sourcePlan, int count) => sourcePlan.AcceptedSkip == count && !sourcePlan.ResidualSkip.HasValue;
    private static bool CanRemoveTake(SourcePlanResult sourcePlan, int count) => sourcePlan.AcceptedTake == count && !sourcePlan.ResidualTake.HasValue;
    private static string RequireSourceContextId(PhysicalSchemaScanNode scan) => scan.SourceContextId ?? throw new InvalidOperationException("Source planning rewrite requires a source context id.");

    private static void AcceptOrder(
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations,
        string sourceContextId,
        IReadOnlyList<OrderByExpression> orderBy)
    {
        if (acceptedOperations.TryGetValue(sourceContextId, out var operations))
            operations.AcceptedOrderBy = orderBy;
    }

    private static void AcceptSkip(
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations,
        string sourceContextId,
        long? skip)
    {
        if (acceptedOperations.TryGetValue(sourceContextId, out var operations))
            operations.AcceptedSkip = skip;
    }

    private static void AcceptTake(
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations,
        string sourceContextId,
        long? take)
    {
        if (acceptedOperations.TryGetValue(sourceContextId, out var operations))
            operations.AcceptedTake = take;
    }

    private static Dictionary<string, SourcePlanResult> RewriteSourcePlanResults(
        IReadOnlyDictionary<string, SourcePlanResult> sourcePlanResults,
        IReadOnlyDictionary<string, AcceptedSourceOperations> acceptedOperations)
    {
        var result = new Dictionary<string, SourcePlanResult>(sourcePlanResults.Count, StringComparer.Ordinal);

        foreach (var entry in sourcePlanResults)
        {
            var sourcePlan = entry.Value;
            var accepted = acceptedOperations[entry.Key];
            var executionPlan = new SourceExecutionPlan
            {
                Identity = accepted.Identity,
                AcceptedColumns = accepted.AcceptedColumns,
                AcceptedPredicate = accepted.AcceptedPredicate,
                AcceptedOrderBy = accepted.AcceptedOrderBy,
                AcceptedSkip = accepted.AcceptedSkip,
                AcceptedTake = accepted.AcceptedTake,
                Properties = accepted.Properties
            };

            result[entry.Key] = sourcePlan with
            {
                ExecutionPlan = executionPlan,
                AcceptedColumns = accepted.AcceptedColumns,
                AcceptedPredicate = accepted.AcceptedPredicate,
                AcceptedOrderBy = accepted.AcceptedOrderBy,
                ResidualOrderBy = CreateResidualOrderBy(sourcePlan, accepted),
                AcceptedSkip = accepted.AcceptedSkip,
                ResidualSkip = CreateResidualSkip(sourcePlan, accepted),
                AcceptedTake = accepted.AcceptedTake,
                ResidualTake = CreateResidualTake(sourcePlan, accepted)
            };
        }

        return result;
    }

    private static IReadOnlyList<OrderByExpression> CreateResidualOrderBy(SourcePlanResult sourcePlan, AcceptedSourceOperations accepted) =>
        accepted.AcceptedOrderBy.Count > 0 ? sourcePlan.ResidualOrderBy : sourcePlan.AcceptedOrderBy.Concat(sourcePlan.ResidualOrderBy).ToArray();

    private static long? CreateResidualSkip(SourcePlanResult sourcePlan, AcceptedSourceOperations accepted) =>
        accepted.AcceptedSkip.HasValue ? sourcePlan.ResidualSkip : sourcePlan.AcceptedSkip ?? sourcePlan.ResidualSkip;

    private static long? CreateResidualTake(SourcePlanResult sourcePlan, AcceptedSourceOperations accepted) =>
        accepted.AcceptedTake.HasValue ? sourcePlan.ResidualTake : sourcePlan.AcceptedTake ?? sourcePlan.ResidualTake;

    private sealed class AcceptedSourceOperations(
        SourceIdentity identity,
        IReadOnlyList<SourceColumnRef> acceptedColumns,
        SourcePredicateExpression? acceptedPredicate,
        IReadOnlyDictionary<string, object?> properties)
    {
        public SourceIdentity Identity { get; } = identity;

        public IReadOnlyList<SourceColumnRef> AcceptedColumns { get; } = acceptedColumns;

        public SourcePredicateExpression? AcceptedPredicate { get; } = acceptedPredicate;

        public IReadOnlyDictionary<string, object?> Properties { get; } = properties;

        public IReadOnlyList<OrderByExpression> AcceptedOrderBy { get; set; } = [];

        public long? AcceptedSkip { get; set; }

        public long? AcceptedTake { get; set; }
    }
}
