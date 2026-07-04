using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.IR.Planning;

internal static class ExecutionStrategyPlanner
{
    public static ExecutionStrategyPlanningResult Plan(
        PhysicalNode physicalPlan,
        CompilationOptions compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        IPlanningShapeResolver shapeResolver)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        ArgumentNullException.ThrowIfNull(shapeResolver);
        var identityMap = PhysicalNodeIdentityMap.Build(physicalPlan);
        var state = new ExecutionStrategyPlanningState(compilationOptions, cteExecutionPlan, shapeResolver, identityMap);
        state.Visit(physicalPlan);

        return new ExecutionStrategyPlanningResult(state.CreatePlan(), state.Decisions);
    }

    private sealed class ExecutionStrategyPlanningState(
        CompilationOptions compilationOptions,
        CteExecutionPlan? cteExecutionPlan,
        IPlanningShapeResolver shapeResolver,
        PhysicalNodeIdentityMap identityMap)
    {
        private readonly ParallelStrategyPlanner _parallelStrategyPlanner = new(
            compilationOptions,
            cteExecutionPlan,
            shapeResolver);
        private readonly Dictionary<PhysicalCteNode, CteStrategyDecision> _cteStrategies = new(ReferenceComparer<PhysicalCteNode>.Instance);
        private readonly Dictionary<PhysicalCteNode, CteSidecarIndexPlan> _cteSidecarIndexPlans = new(ReferenceComparer<PhysicalCteNode>.Instance);
        private readonly Dictionary<PhysicalSetOperationNode, SetOperationStrategyDecision> _setOperationStrategies = new(ReferenceComparer<PhysicalSetOperationNode>.Instance);
        private readonly List<PlanningDecision> _decisions = [];
        private int _nextCteSidecarIndexSlot;

        public IReadOnlyList<PlanningDecision> Decisions => _decisions;

        public void Visit(PhysicalNode node)
        {
            if (ExecutionStrategyPipelineDecomposer.TryDecomposeSingleKeyAggregatePipeline(node) is { } aggregatePipeline)
                AddDecision(_parallelStrategyPlanner.AddAggregateCandidate(aggregatePipeline));

            if (ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(node) is { } supportedPipeline)
                AddDecision(_parallelStrategyPlanner.AddFilterProjectCandidate(supportedPipeline));

            if (node is PhysicalCteNode cte)
            {
                AddCteStrategy(cte);
                AddCteSidecarIndexes(cte);
                AddDecision(_parallelStrategyPlanner.AddCteLevels(cte));
            }

            if (node is PhysicalSetOperationNode setOperation)
                AddSetOperationStrategy(setOperation);

            foreach (var child in node.Children)
                Visit(child);
        }

        public ExecutionStrategyPlan CreatePlan()
        {
            return ExecutionStrategyPlan.Create(
                identityMap,
                _parallelStrategyPlanner.AggregateCandidates,
                _parallelStrategyPlanner.FilterProjectCandidates,
                _parallelStrategyPlanner.CteLevels,
                _cteStrategies,
                _cteSidecarIndexPlans,
                _setOperationStrategies,
                new Dictionary<string, SourceBoundaryStrategyPlan>(StringComparer.Ordinal),
                new Dictionary<BoundaryRowShapeKind, RowWidthPruningPlan[]>(),
                []);
        }

        private void AddCteStrategy(PhysicalCteNode node)
        {
            if (_cteStrategies.ContainsKey(node))
                return;

            var strategy = CteStrategyPlanner.Choose(node);
            _cteStrategies.Add(node, strategy);

            foreach (var definition in strategy.DefinitionsByName.Values)
            {
                _decisions.Add(new PlanningDecision(
                    PlanningDecisionCategory.CteStrategy,
                    "CteReuseStrategy",
                    $"cte:{definition.Name}",
                    definition.Kind.ToString(),
                    PlanningConfidence.High,
                    definition.Reason));
            }
        }

        private void AddCteSidecarIndexes(PhysicalCteNode node)
        {
            if (_cteSidecarIndexPlans.ContainsKey(node))
                return;

            var strategy = _cteStrategies.TryGetValue(node, out var planned)
                ? planned
                : CteStrategyPlanner.Choose(node);
            var result = CteSidecarIndexPlanner.Choose(
                node,
                compilationOptions,
                strategy,
                _nextCteSidecarIndexSlot,
                _parallelStrategyPlanner.EvaluateFilterProjectEligibility);

            _nextCteSidecarIndexSlot = result.NextIndexSlot;
            if (result.Plan.DefinitionsByName.Count > 0 || result.Plan.ConsumersByJoin.Count > 0)
                _cteSidecarIndexPlans.Add(node, result.Plan);

            _decisions.AddRange(result.Decisions);
        }

        private void AddSetOperationStrategy(PhysicalSetOperationNode node)
        {
            if (_setOperationStrategies.ContainsKey(node))
                return;

            var strategy = SetOperationStrategyPlanner.Choose(node);
            _setOperationStrategies.Add(node, strategy);
            _decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.SetOperationStrategy,
                "SetOperationStrategy",
                node.Kind.ToString(),
                strategy.Outcome,
                PlanningConfidence.High,
                strategy.Reason));
        }

        private void AddDecision(PlanningDecision? decision)
        {
            if (decision != null)
                _decisions.Add(decision);
        }
    }
}
