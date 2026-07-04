using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class MaterializationPlanner
{
    public static IReadOnlyList<PlanningDecision> Plan(
        PhysicalNode physicalPlan,
        ExecutionStrategyPlan executionStrategies)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(executionStrategies);
        var decisions = new List<PlanningDecision>(SingleUseMaterializationPlanner.Plan(physicalPlan));
        Visit(physicalPlan, null, executionStrategies, decisions);

        return decisions;
    }

    private static void Visit(
        PhysicalNode node,
        PhysicalNode? parent,
        ExecutionStrategyPlan executionStrategies,
        List<PlanningDecision> decisions)
    {
        AddNodeDecision(node, parent, executionStrategies, decisions);

        foreach (var child in node.Children)
            Visit(child, node, executionStrategies, decisions);
    }

    private static void AddNodeDecision(
        PhysicalNode node,
        PhysicalNode? parent,
        ExecutionStrategyPlan executionStrategies,
        List<PlanningDecision> decisions)
    {
        switch (node)
        {
            case PhysicalMaterializeNode:
                AddDecision(
                    decisions,
                    "MaterializationBoundary",
                    ResolveMaterializeTarget(parent),
                    "Required",
                    ResolveMaterializeReason(parent));
                break;
            case PhysicalSortNode sort:
                AddDecision(
                    decisions,
                    "OrderingBoundary",
                    "PhysicalSortNode",
                    "Required",
                    $"Sort materializes rows before applying {sort.Keys.Length.ToString(CultureInfo.InvariantCulture)} order key(s).");
                break;
            case PhysicalTopNNode topN:
                AddDecision(
                    decisions,
                    "TopNBoundary",
                    "PhysicalTopNNode",
                    "Required",
                    $"Top-N keeps an ordered materialization for {topN.N.ToString(CultureInfo.InvariantCulture)} row(s).");
                break;
            case PhysicalTopOffsetNode topOffset:
                AddDecision(
                    decisions,
                    "TopOffsetBoundary",
                    "PhysicalTopOffsetNode",
                    "Required",
                    $"Top-offset keeps an ordered materialization after skipping {topOffset.Skip.ToString(CultureInfo.InvariantCulture)} row(s).");
                break;
            case PhysicalAggregateOnlyNode:
            case PhysicalSingleKeyAggregateNode:
            case PhysicalValueTupleAggregateNode:
                AddDecision(
                    decisions,
                    "AggregateBoundary",
                    node.GetType().Name,
                    "Required",
                    "Aggregate planning materializes group state before final projection.");
                break;
            case PhysicalProjectNode { IsDistinct: true }:
                AddDecision(
                    decisions,
                    "DistinctBoundary",
                    "PhysicalProjectNode",
                    "Required",
                    "Distinct projection materializes rows before duplicate removal.");
                break;
            case PhysicalSetOperationNode setOperation:
                AddDecision(
                    decisions,
                    "SetOperationBoundary",
                    setOperation.Kind.ToString(),
                    "Required",
                    "Set operation materializes row identity while combining input tables.");
                break;
            case PhysicalWindowNode window:
                AddDecision(
                    decisions,
                    "WindowBoundary",
                    "PhysicalWindowNode",
                    "Required",
                    $"Window planning materializes input rows for {window.Registrations.Length.ToString(CultureInfo.InvariantCulture)} window registration(s).");
                break;
            case PhysicalHashJoinNode:
                AddDecision(
                    decisions,
                    "HashJoinBuildBoundary",
                    "PhysicalHashJoinNode",
                    "Required",
                    "Hash join materializes the build side into a hash table before probing.");
                break;
            case PhysicalNestedLoopApplyNode:
                AddDecision(
                    decisions,
                    "ApplyBoundary",
                    "PhysicalNestedLoopApplyNode",
                    "Required",
                    "APPLY evaluates a lateral right source for each left row and preserves that boundary in the plan.");
                break;
            case PhysicalInterpretSourceNode interpret:
                AddDecision(
                    decisions,
                    "InterpretationBoundary",
                    interpret.Kind.ToString(),
                    "Required",
                    "Interpretation source creates typed rows from source expressions before projection.");
                break;
            case PhysicalCteNode cte:
                AddCteDecisions(cte, executionStrategies, decisions);
                break;
        }
    }

    private static void AddCteDecisions(
        PhysicalCteNode cte,
        ExecutionStrategyPlan executionStrategies,
        List<PlanningDecision> decisions)
    {
        var strategy = executionStrategies.GetCteStrategy(cte);

        foreach (var definition in strategy.DefinitionsByName.Values)
        {
            AddDecision(
                decisions,
                ResolveCteMaterializationRuleName(definition),
                $"cte:{definition.Name}",
                ResolveCteMaterializationOutcome(definition),
                definition.Reason);
        }
    }

    private static string ResolveCteMaterializationRuleName(CteDefinitionStrategyDecision definition)
    {
        return definition.Kind switch
        {
            CteDefinitionStrategyKind.MaterializeReuse => "CteReuseBoundary",
            CteDefinitionStrategyKind.FuseReadOnce => "CteFusionBoundary",
            _ => "CteBoundary"
        };
    }

    private static string ResolveCteMaterializationOutcome(CteDefinitionStrategyDecision definition)
    {
        return definition.Kind switch
        {
            CteDefinitionStrategyKind.Unreferenced => "Skipped",
            CteDefinitionStrategyKind.FuseReadOnce => "Candidate",
            CteDefinitionStrategyKind.MaterializeReuse => "Required",
            _ => "Conditional"
        };
    }

    private static string ResolveMaterializeTarget(PhysicalNode? parent)
    {
        return parent switch
        {
            PhysicalWindowNode => "window-input",
            PhysicalSetOperationNode => "set-operation-input",
            PhysicalNestedLoopApplyNode => "apply-input",
            null => "physical-materialize",
            _ => parent.GetType().Name
        };
    }

    private static string ResolveMaterializeReason(PhysicalNode? parent)
    {
        return parent switch
        {
            PhysicalWindowNode => "Window strategy requires a stable materialized input before computing rankings or offsets.",
            PhysicalSetOperationNode => "Set operation requires stable input rows for membership evaluation.",
            PhysicalNestedLoopApplyNode => "APPLY strategy preserves the lateral source boundary before lowering nested loops.",
            null => "Physical plan contains an explicit materialization boundary.",
            _ => $"{parent.GetType().Name} requires an explicit materialization boundary."
        };
    }

    private static void AddDecision(
        List<PlanningDecision> decisions,
        string ruleName,
        string target,
        string outcome,
        string reason)
    {
        decisions.Add(new PlanningDecision(
            PlanningDecisionCategory.Materialization,
            ruleName,
            target,
            outcome,
            PlanningConfidence.High,
            reason));
    }
}
