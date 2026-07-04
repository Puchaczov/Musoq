using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial class ParallelStrategyPlanner(
    CompilationOptions compilationOptions,
    CteExecutionPlan? cteExecutionPlan,
    IPlanningShapeResolver shapeResolver)
{
    private readonly HashSet<PhysicalSingleKeyAggregateNode> _aggregateCandidates = new(ReferenceComparer<PhysicalSingleKeyAggregateNode>.Instance);
    private readonly HashSet<PhysicalProjectNode> _filterProjectCandidates = new(ReferenceComparer<PhysicalProjectNode>.Instance);
    private readonly Dictionary<PhysicalCteNode, IReadOnlyList<PlannedParallelCteLevel>> _cteLevels = new(ReferenceComparer<PhysicalCteNode>.Instance);
    private readonly HashSet<PhysicalSingleKeyAggregateNode> _visitedAggregates = new(ReferenceComparer<PhysicalSingleKeyAggregateNode>.Instance);
    private readonly HashSet<PhysicalProjectNode> _visitedFilterProjects = new(ReferenceComparer<PhysicalProjectNode>.Instance);

    public IReadOnlySet<PhysicalSingleKeyAggregateNode> AggregateCandidates => _aggregateCandidates;

    public IReadOnlySet<PhysicalProjectNode> FilterProjectCandidates => _filterProjectCandidates;

    public IReadOnlyDictionary<PhysicalCteNode, IReadOnlyList<PlannedParallelCteLevel>> CteLevels => _cteLevels;

    public ParallelPlanEligibility EvaluateFilterProjectEligibility(SupportedPipeline pipeline)
    {
        return EvaluateParallelFilterProject(pipeline);
    }

    public PlanningDecision? AddAggregateCandidate(SingleKeyAggregatePipeline pipeline)
    {
        if (!_visitedAggregates.Add(pipeline.Aggregate))
            return null;

        var eligibility = EvaluateParallelAggregate(pipeline);
        if (!eligibility.IsEligible)
        {
            return CreateParallelDecision(
                "ParallelSingleKeyAggregate",
                pipeline.Aggregate.GetType().Name,
                eligibility.Outcome,
                PlanningConfidence.High,
                eligibility.Reason);
        }

        _aggregateCandidates.Add(pipeline.Aggregate);
        return CreateParallelDecision(
            "ParallelSingleKeyAggregate",
            pipeline.Aggregate.GetType().Name,
            "Enabled",
            PlanningConfidence.High,
            eligibility.Reason);
    }

    public PlanningDecision? AddFilterProjectCandidate(SupportedPipeline pipeline)
    {
        if (!_visitedFilterProjects.Add(pipeline.Project))
            return null;

        var eligibility = EvaluateParallelFilterProject(pipeline);
        if (!eligibility.IsEligible)
        {
            return CreateParallelDecision(
                "ParallelFilterProject",
                pipeline.Project.GetType().Name,
                eligibility.Outcome,
                PlanningConfidence.High,
                eligibility.Reason);
        }

        _filterProjectCandidates.Add(pipeline.Project);
        return CreateParallelDecision(
            "ParallelFilterProject",
            pipeline.Project.GetType().Name,
            "Enabled",
            PlanningConfidence.High,
            eligibility.Reason);
    }

    public PlanningDecision AddCteLevels(PhysicalCteNode node)
    {
        var levels = TryPlanParallelCteLevels(node, out var reason);
        if (levels.Count == 0)
        {
            return CreateParallelDecision(
                "ParallelCte",
                node.GetType().Name,
                "Skipped",
                PlanningConfidence.High,
                reason);
        }

        _cteLevels.Add(node, levels);
        return CreateParallelDecision(
            "ParallelCte",
            node.GetType().Name,
            "Candidate",
            PlanningConfidence.High,
            reason);
    }


}
