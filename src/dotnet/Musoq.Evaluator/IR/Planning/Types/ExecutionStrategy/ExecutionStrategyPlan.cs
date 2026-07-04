using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ExecutionStrategyPlan
{
    private readonly PhysicalNodeIdentityMap identityMap;
    private readonly IReadOnlySet<PhysicalNodeId> parallelAggregateCandidateIds;
    private readonly IReadOnlySet<PhysicalNodeId> parallelFilterProjectCandidateIds;
    private readonly IReadOnlyDictionary<PhysicalNodeId, IReadOnlyList<PlannedParallelCteLevel>> parallelCteLevels;
    private readonly IReadOnlyDictionary<PhysicalNodeId, CteStrategyDecision> cteStrategies;
    private readonly IReadOnlyDictionary<PhysicalNodeId, CteSidecarIndexPlan> cteSidecarIndexPlans;
    private readonly IReadOnlyDictionary<PhysicalNodeId, CteSidecarIndexSpec> cteSidecarConsumersByJoinId;
    private readonly IReadOnlyDictionary<PhysicalNodeId, SetOperationStrategyDecision> setOperationStrategies;
    private readonly IReadOnlyDictionary<string, SourceBoundaryStrategyPlan> sourceBoundaryStrategies;
    private readonly IReadOnlyDictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]> rowWidthPruningPlans;
    private readonly IReadOnlyList<CardinalityFact> cardinalityFacts;

    public ExecutionStrategyPlan(
        PhysicalNodeIdentityMap identityMap,
        IReadOnlySet<PhysicalNodeId> parallelAggregateCandidateIds,
        IReadOnlySet<PhysicalNodeId> parallelFilterProjectCandidateIds,
        IReadOnlyDictionary<PhysicalNodeId, IReadOnlyList<PlannedParallelCteLevel>> parallelCteLevels,
        IReadOnlyDictionary<PhysicalNodeId, CteStrategyDecision> cteStrategies,
        IReadOnlyDictionary<PhysicalNodeId, CteSidecarIndexPlan> cteSidecarIndexPlans,
        IReadOnlyDictionary<PhysicalNodeId, CteSidecarIndexSpec> cteSidecarConsumersByJoinId,
        IReadOnlyDictionary<PhysicalNodeId, SetOperationStrategyDecision> setOperationStrategies,
        IReadOnlyDictionary<string, SourceBoundaryStrategyPlan> sourceBoundaryStrategies,
        IReadOnlyDictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]> rowWidthPruningPlans,
        IReadOnlyList<CardinalityFact> cardinalityFacts)
    {
        ArgumentNullException.ThrowIfNull(identityMap);
        ArgumentNullException.ThrowIfNull(parallelAggregateCandidateIds);
        ArgumentNullException.ThrowIfNull(parallelFilterProjectCandidateIds);
        ArgumentNullException.ThrowIfNull(parallelCteLevels);
        ArgumentNullException.ThrowIfNull(cteStrategies);
        ArgumentNullException.ThrowIfNull(cteSidecarIndexPlans);
        ArgumentNullException.ThrowIfNull(cteSidecarConsumersByJoinId);
        ArgumentNullException.ThrowIfNull(setOperationStrategies);
        ArgumentNullException.ThrowIfNull(sourceBoundaryStrategies);
        ArgumentNullException.ThrowIfNull(rowWidthPruningPlans);
        ArgumentNullException.ThrowIfNull(cardinalityFacts);

        this.identityMap = identityMap;
        this.parallelAggregateCandidateIds = parallelAggregateCandidateIds;
        this.parallelFilterProjectCandidateIds = parallelFilterProjectCandidateIds;
        this.parallelCteLevels = parallelCteLevels;
        this.cteStrategies = cteStrategies;
        this.cteSidecarIndexPlans = cteSidecarIndexPlans;
        this.cteSidecarConsumersByJoinId = cteSidecarConsumersByJoinId;
        this.setOperationStrategies = setOperationStrategies;
        this.sourceBoundaryStrategies = sourceBoundaryStrategies;
        this.rowWidthPruningPlans = rowWidthPruningPlans;
        this.cardinalityFacts = cardinalityFacts;
    }

    public static ExecutionStrategyPlan Create(
        PhysicalNodeIdentityMap identityMap,
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
        ArgumentNullException.ThrowIfNull(identityMap);

        return new ExecutionStrategyPlan(
            identityMap,
            ToIdSet(identityMap, parallelAggregateCandidates),
            ToIdSet(identityMap, parallelFilterProjectCandidates),
            ToIdDictionary(identityMap, parallelCteLevels),
            ToIdDictionary(identityMap, cteStrategies),
            ToIdDictionary(identityMap, cteSidecarIndexPlans),
            ToCteSidecarConsumerIds(identityMap, cteSidecarIndexPlans.Values),
            ToIdDictionary(identityMap, setOperationStrategies),
            sourceBoundaryStrategies,
            rowWidthPruningPlans,
            cardinalityFacts);
    }

    public ExecutionStrategyPlan WithSourceBoundaryStrategies(IReadOnlyList<SourceBoundaryStrategyPlan> strategies)
    {
        var strategiesByBoundaryId = new Dictionary<string, SourceBoundaryStrategyPlan>(StringComparer.Ordinal);

        foreach (var strategy in strategies)
            strategiesByBoundaryId[strategy.BoundaryId] = strategy;

        return new ExecutionStrategyPlan(
            identityMap,
            parallelAggregateCandidateIds,
            parallelFilterProjectCandidateIds,
            parallelCteLevels,
            cteStrategies,
            cteSidecarIndexPlans,
            cteSidecarConsumersByJoinId,
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
            identityMap,
            parallelAggregateCandidateIds,
            parallelFilterProjectCandidateIds,
            parallelCteLevels,
            cteStrategies,
            cteSidecarIndexPlans,
            cteSidecarConsumersByJoinId,
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
        return identityMap.TryGetId(node, out var id) &&
               parallelAggregateCandidateIds.Contains(id);
    }

    public bool CanUseParallelFilterProject(PhysicalProjectNode node)
    {
        return identityMap.TryGetId(node, out var id) &&
               parallelFilterProjectCandidateIds.Contains(id);
    }

    public bool TryResolvePhysicalNodeId(PhysicalNode node, out PhysicalNodeId id)
    {
        return identityMap.TryGetId(node, out id);
    }

    public IReadOnlyList<PlannedParallelCteLevel> GetParallelCteLevels(PhysicalCteNode node)
    {
        return identityMap.TryGetId(node, out var id) &&
               parallelCteLevels.TryGetValue(id, out var levels)
            ? levels
            : [];
    }

    public CteStrategyDecision GetCteStrategy(PhysicalCteNode node)
    {
        return identityMap.TryGetId(node, out var id) &&
               cteStrategies.TryGetValue(id, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No CTE strategy was planned for {node.GetType().Name}.");
    }

    public IReadOnlyList<CteSidecarIndexSpec> GetCteSidecarIndexSpecs(PhysicalCteNode node, string definitionName)
    {
        if (!identityMap.TryGetId(node, out var id) ||
            !cteSidecarIndexPlans.TryGetValue(id, out var plan))
        {
            return [];
        }

        return plan.DefinitionsByName.TryGetValue(definitionName, out var specs) ? specs : [];
    }

    public bool TryGetCteSidecarIndexConsumer(PhysicalHashJoinNode join, [NotNullWhen(true)] out CteSidecarIndexSpec? spec)
    {
        if (identityMap.TryGetId(join, out var id) &&
            cteSidecarConsumersByJoinId.TryGetValue(id, out spec))
        {
            return true;
        }

        spec = null;
        return false;
    }

    public SetOperationStrategyDecision GetSetOperationStrategy(PhysicalSetOperationNode node)
    {
        return identityMap.TryGetId(node, out var id) &&
               setOperationStrategies.TryGetValue(id, out var strategy)
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

    private static HashSet<PhysicalNodeId> ToIdSet<TNode>(
        PhysicalNodeIdentityMap identityMap,
        IEnumerable<TNode> nodes)
        where TNode : PhysicalNode
    {
        var result = new HashSet<PhysicalNodeId>();

        foreach (var node in nodes)
            result.Add(identityMap.GetId(node));

        return result;
    }

    private static Dictionary<PhysicalNodeId, TValue> ToIdDictionary<TNode, TValue>(
        PhysicalNodeIdentityMap identityMap,
        IReadOnlyDictionary<TNode, TValue> source)
        where TNode : PhysicalNode
    {
        var result = new Dictionary<PhysicalNodeId, TValue>();

        foreach (var entry in source)
            result[identityMap.GetId(entry.Key)] = entry.Value;

        return result;
    }

    private static Dictionary<PhysicalNodeId, CteSidecarIndexSpec> ToCteSidecarConsumerIds(
        PhysicalNodeIdentityMap identityMap,
        IEnumerable<CteSidecarIndexPlan> plans)
    {
        var result = new Dictionary<PhysicalNodeId, CteSidecarIndexSpec>();

        foreach (var plan in plans)
        foreach (var entry in plan.ConsumersByJoin)
            result[identityMap.GetId(entry.Key)] = entry.Value;

        return result;
    }
}
