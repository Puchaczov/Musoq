using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class CteSidecarIndexPlanner
{
    public static CteSidecarIndexPlanningResult Choose(
        PhysicalCteNode node,
        CompilationOptions options,
        CteStrategyDecision cteStrategy,
        int nextIndexSlot,
        Func<SupportedPipeline, ParallelPlanEligibility>? evaluateParallelFilterProject = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cteStrategy);

        if (!options.UseCteSidecarIndexes || !options.UseHashJoin)
            return CteSidecarIndexPlanningResult.Empty(nextIndexSlot);

        var decisions = new List<PlanningDecision>();
        var definitionsByName = new Dictionary<string, IReadOnlyList<CteSidecarIndexSpec>>(StringComparer.OrdinalIgnoreCase);
        var consumersByJoin = new Dictionary<PhysicalHashJoinNode, CteSidecarIndexSpec>(ReferenceComparer<PhysicalHashJoinNode>.Instance);
        var specsByStableKey = new Dictionary<string, CteSidecarIndexSpec>(StringComparer.OrdinalIgnoreCase);
        var currentSlot = nextIndexSlot;

        for (var definitionIndex = 0; definitionIndex < node.Definitions.Length; definitionIndex++)
        {
            var definition = node.Definitions[definitionIndex];
            if (!TryGetDefinitionEligibility(definition, cteStrategy, evaluateParallelFilterProject, out var skipReason))
            {
                decisions.Add(CreateSkippedDecision(definition.Name, "definition", skipReason));
                continue;
            }

            foreach (var consumer in FindLaterDirectConsumers(node, definitionIndex, definition.Name))
            {
                if (consumer.HashJoin == null || consumer.CteRef == null)
                {
                    decisions.Add(CreateSkippedDecision(
                        definition.Name,
                        consumer.HashJoin == null ? "join" : CreateConsumerTarget(definition.Name, consumer.HashJoin),
                        string.IsNullOrWhiteSpace(consumer.SkipReason)
                            ? "Consumer is not an eligible direct hash-build CTE reference."
                            : consumer.SkipReason));
                    continue;
                }

                if (!TryCreateConsumerSpec(
                        definition.Name,
                        definition.Plan.OutputSchema,
                        consumer.HashJoin,
                        consumer.CteRef,
                        currentSlot,
                        out var candidate,
                        out var consumerSkipReason))
                {
                    decisions.Add(CreateSkippedDecision(
                        definition.Name,
                        CreateConsumerTarget(definition.Name, consumer.HashJoin),
                        string.IsNullOrWhiteSpace(consumer.SkipReason) ? consumerSkipReason : consumer.SkipReason));
                    continue;
                }

                var stableKey = candidate.StableKey;
                if (!specsByStableKey.TryGetValue(stableKey, out var spec))
                {
                    spec = candidate;
                    specsByStableKey.Add(stableKey, spec);
                    currentSlot++;
                }

                consumersByJoin[consumer.HashJoin] = spec;
                decisions.Add(CreateSelectedDecision(spec, consumer.HashJoin, ReferenceEquals(spec, candidate)));
            }
        }

        foreach (var group in specsByStableKey.Values
                     .GroupBy(static spec => spec.CteName, StringComparer.OrdinalIgnoreCase))
        {
            definitionsByName[group.Key] = group
                .OrderBy(static spec => spec.IndexSlot)
                .ToArray();
        }

        if (definitionsByName.Count == 0 && consumersByJoin.Count == 0)
            return new CteSidecarIndexPlanningResult(CteSidecarIndexPlan.Empty, decisions, currentSlot);

        return new CteSidecarIndexPlanningResult(
            new CteSidecarIndexPlan(definitionsByName, consumersByJoin),
            decisions,
            currentSlot);
    }

    public static string CreateStableKey(
        string cteName,
        CteSidecarIndexKind kind,
        IReadOnlyList<string> keyColumns)
    {
        return string.Join(
            "|",
            cteName,
            kind.ToString(),
            string.Join("\u001f", keyColumns));
    }

    private static bool TryGetDefinitionEligibility(
        PhysicalCteDefinition definition,
        CteStrategyDecision cteStrategy,
        Func<SupportedPipeline, ParallelPlanEligibility>? evaluateParallelFilterProject,
        out string reason)
    {
        if (GeneratedSubqueryContract.IsGeneratedSubqueryCteName(definition.Name))
        {
            reason = "Generated subquery CTEs keep their existing specialized lowering.";
            return false;
        }

        if (!cteStrategy.DefinitionsByName.TryGetValue(definition.Name, out var strategy))
        {
            reason = "No CTE reuse strategy was planned for this definition.";
            return false;
        }

        if (strategy.Kind is not (CteDefinitionStrategyKind.MaterializeSingleUse or CteDefinitionStrategyKind.MaterializeReuse))
        {
            reason = $"CTE strategy is {strategy.Kind}, so there is no materialized table append path to decorate.";
            return false;
        }

        var pipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(
            ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(definition.Plan));
        if (pipeline == null)
        {
            reason = "CTE output is not a supported direct filter/project table pipeline for sidecar index production.";
            return false;
        }

        if (pipeline.Project.IsDistinct)
        {
            reason = "CTE output uses DISTINCT, which rewrites the final stored table after the append path.";
            return false;
        }

        if (pipeline.PostOperations.Count != 0)
        {
            reason = "CTE output has final post-operations, so the stored table is rewritten after row production.";
            return false;
        }

        if (evaluateParallelFilterProject?.Invoke(pipeline) is { IsEligible: true })
        {
            reason = "CTE output uses parallel filter/project lowering, whose append path is not sidecar-index safe.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static IEnumerable<CteHashJoinConsumer> FindLaterDirectConsumers(
        PhysicalCteNode cteNode,
        int definitionIndex,
        string cteName)
    {
        for (var index = definitionIndex + 1; index < cteNode.Definitions.Length; index++)
        {
            foreach (var consumer in FindDirectConsumers(cteNode.Definitions[index].Plan, cteName))
                yield return consumer;
        }

        foreach (var consumer in FindDirectConsumers(cteNode.Query, cteName))
            yield return consumer;
    }

    private static IEnumerable<CteHashJoinConsumer> FindDirectConsumers(PhysicalNode node, string cteName)
    {
        switch (node)
        {
            case PhysicalHashJoinNode hashJoin:
                if (hashJoin.Kind == JoinKind.FullOuter)
                {
                    if (HasDirectCteRef(hashJoin.Left, cteName) || HasDirectCteRef(hashJoin.Right, cteName))
                        yield return new CteHashJoinConsumer(null, null, "Full outer hash joins track per-build-row match state and do not use CTE sidecar indexes.");

                    break;
                }

                if (TryFindDirectBuildCteRef(hashJoin, cteName, out var cteRef, out var skipReason))
                {
                    yield return new CteHashJoinConsumer(hashJoin, cteRef);
                }
                else if (HasDirectCteRef(hashJoin.Left, cteName) || HasDirectCteRef(hashJoin.Right, cteName))
                {
                    yield return new CteHashJoinConsumer(hashJoin, null, skipReason);
                }

                break;
            case PhysicalSortMergeJoinNode sortMergeJoin:
                if (HasDirectCteRef(sortMergeJoin.Left, cteName) || HasDirectCteRef(sortMergeJoin.Right, cteName))
                    yield return new CteHashJoinConsumer(null, null, "Sort-merge joins keep their existing range/order-oriented lowering.");
                break;
            case PhysicalNestedLoopJoinNode nestedLoopJoin:
                if (HasDirectCteRef(nestedLoopJoin.Left, cteName) || HasDirectCteRef(nestedLoopJoin.Right, cteName))
                    yield return new CteHashJoinConsumer(null, null, "Nested-loop joins do not use a hash-build consumer.");
                break;
        }

        foreach (var child in node.Children)
        {
            foreach (var consumer in FindDirectConsumers(child, cteName))
                yield return consumer;
        }
    }

    private static bool TryFindDirectBuildCteRef(
        PhysicalHashJoinNode hashJoin,
        string cteName,
        out PhysicalCteRefNode cteRef,
        out string reason)
    {
        var candidates = new List<PhysicalCteRefNode>(2);

        if (hashJoin.Left is PhysicalCteRefNode left &&
            string.Equals(left.CteName, cteName, StringComparison.OrdinalIgnoreCase) &&
            HashBuildAliasUsage.BuildKeysReferenceAlias(hashJoin, left.Alias))
        {
            candidates.Add(left);
        }

        if (hashJoin.Right is PhysicalCteRefNode right &&
            string.Equals(right.CteName, cteName, StringComparison.OrdinalIgnoreCase) &&
            HashBuildAliasUsage.BuildKeysReferenceAlias(hashJoin, right.Alias))
        {
            candidates.Add(right);
        }

        if (candidates.Count == 1)
        {
            cteRef = candidates[0];
            reason = string.Empty;
            return true;
        }

        cteRef = null!;
        reason = candidates.Count == 0
            ? "The CTE is not the direct physical hash-build side for this consumer."
            : "The hash-build side is ambiguous for this CTE consumer.";
        return false;
    }

    private static bool HasDirectCteRef(PhysicalNode node, string cteName)
    {
        return node is PhysicalCteRefNode cteRef &&
               string.Equals(cteRef.CteName, cteName, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct CteHashJoinConsumer(
        PhysicalHashJoinNode? HashJoin,
        PhysicalCteRefNode? CteRef,
        string SkipReason = "");
}
