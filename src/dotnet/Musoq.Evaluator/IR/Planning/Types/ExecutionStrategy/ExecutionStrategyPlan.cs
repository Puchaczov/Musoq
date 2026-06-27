using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ExecutionStrategyPlan(
    IReadOnlySet<PhysicalSingleKeyAggregateNode> parallelAggregateCandidates,
    IReadOnlySet<PhysicalProjectNode> parallelFilterProjectCandidates,
    IReadOnlyDictionary<PhysicalCteNode, IReadOnlyList<PlannedParallelCteLevel>> parallelCteLevels,
    IReadOnlyDictionary<PhysicalCteNode, CteStrategyDecision> cteStrategies,
    IReadOnlyDictionary<PhysicalCteNode, CteSidecarIndexPlan> cteSidecarIndexPlans,
    IReadOnlyDictionary<PhysicalSetOperationNode, SetOperationStrategyDecision> setOperationStrategies,
    IReadOnlyDictionary<string, SourceBoundaryStrategyPlan> sourceBoundaryStrategies,
    IReadOnlyDictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]> rowWidthPruningPlans,
    IReadOnlyList<CardinalityFact> cardinalityFacts)
{
    public ExecutionStrategyPlan WithSourceBoundaryStrategies(IReadOnlyList<SourceBoundaryStrategyPlan> strategies)
    {
        var strategiesByBoundaryId = new Dictionary<string, SourceBoundaryStrategyPlan>(StringComparer.Ordinal);

        foreach (var strategy in strategies)
            strategiesByBoundaryId[strategy.BoundaryId] = strategy;

        return new ExecutionStrategyPlan(
            parallelAggregateCandidates,
            parallelFilterProjectCandidates,
            parallelCteLevels,
            cteStrategies,
            cteSidecarIndexPlans,
            setOperationStrategies,
                strategiesByBoundaryId,
                rowWidthPruningPlans,
                cardinalityFacts);
    }

    public ExecutionStrategyPlan WithRowWidthPruningPlans(IReadOnlyList<RowWidthPruningPlan> plans)
            {
            var plansByKind = plans
                .GroupBy(static plan => plan.Kind)
                .ToDictionary(
                static group => group.Key,
                static group => group.OrderBy(static plan => plan.BoundaryId, StringComparer.Ordinal).ToArray());

            return new ExecutionStrategyPlan(
                parallelAggregateCandidates,
                parallelFilterProjectCandidates,
                parallelCteLevels,
                cteStrategies,
                cteSidecarIndexPlans,
                setOperationStrategies,
                sourceBoundaryStrategies,
                plansByKind,
                cardinalityFacts);
    }

            public bool HasRowWidthPruningPlans => rowWidthPruningPlans.Count > 0;

            public bool HasRowWidthPruningPlan(BoundaryRowShapeKind kind)
            {
                return rowWidthPruningPlans.ContainsKey(kind);
            }

    public bool CanUseParallelSingleKeyAggregate(PhysicalSingleKeyAggregateNode node)
    {
        return parallelAggregateCandidates.Contains(node);
    }

    public bool CanUseParallelFilterProject(PhysicalProjectNode node)
    {
        return parallelFilterProjectCandidates.Contains(node);
    }

    public IReadOnlyList<PlannedParallelCteLevel> GetParallelCteLevels(PhysicalCteNode node)
    {
        return parallelCteLevels.TryGetValue(node, out var levels) ? levels : [];
    }

    public CteStrategyDecision GetCteStrategy(PhysicalCteNode node)
    {
        return cteStrategies.TryGetValue(node, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No CTE strategy was planned for {node.GetType().Name}.");
    }

    public IReadOnlyList<CteSidecarIndexSpec> GetCteSidecarIndexSpecs(PhysicalCteNode node, string definitionName)
    {
        if (!cteSidecarIndexPlans.TryGetValue(node, out var plan))
            return [];

        return plan.DefinitionsByName.TryGetValue(definitionName, out var specs) ? specs : [];
    }

    public bool TryGetCteSidecarIndexConsumer(PhysicalHashJoinNode join, [NotNullWhen(true)] out CteSidecarIndexSpec? spec)
    {
        foreach (var plan in cteSidecarIndexPlans.Values)
        {
            if (plan.ConsumersByJoin.TryGetValue(join, out spec))
                return true;
        }

        spec = null;
        return false;
    }

    public SetOperationStrategyDecision GetSetOperationStrategy(PhysicalSetOperationNode node)
    {
        return setOperationStrategies.TryGetValue(node, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No execution strategy was planned for {node.GetType().Name}.");
    }

    public SourceBoundaryStrategyPlan? GetSourceBoundaryStrategy(string boundaryId)
    {
        return sourceBoundaryStrategies.TryGetValue(boundaryId, out var strategy) ? strategy : null;
    }

    public RowWidthPruningPlan? GetAppliedRowWidthPruning(BoundaryRowShapeKind kind)
    {
        if (!rowWidthPruningPlans.TryGetValue(kind, out var plans))
            return null;

        return plans.FirstOrDefault(static plan => plan.Strategy == RowWidthPruningStrategy.Applied);
    }

}
