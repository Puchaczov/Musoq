using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.SourcePlanning;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PlanningPropertyDeriver
{
    public static PlanningPropertyResult Derive(PlanningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var decisions = new List<PlanningDecision>();
        var scans = CollectSchemaScans(context.LogicalPlan);
        var requiredColumnUsageResult = RequiredColumnUsagePlanner.Plan(context.LogicalPlan);
        decisions.AddRange(requiredColumnUsageResult.Decisions);
        var requiredColumnsByAlias = requiredColumnUsageResult.RequiredColumnsByAlias;
        var sourcePredicatePlanningResult = SourcePredicatePlanner.Plan(context.UsedWhereNodes);
        var pushedPredicates = sourcePredicatePlanningResult.PushedPredicatesBySourceId;
        var preliminaryDecisions = new List<PlanningDecision>();
        var sources = CreateSourceProperties(context, scans, requiredColumnsByAlias, pushedPredicates, preliminaryDecisions);
        var sourceInteractionForMovement = SourceInteractionPlanner.Plan(
            context,
            scans,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        var predicatePlacementPlanningResult = PredicatePlacementPlanner.Plan(
            context.LogicalPlan,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        decisions.AddRange(predicatePlacementPlanningResult.Decisions);
        var predicateMovementPlanningResult = PredicateMovementPlanner.Plan(
            context.LogicalPlan,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId,
            sourceInteractionForMovement.PlansBySourceId);
        decisions.AddRange(predicateMovementPlanningResult.Decisions);

        sourcePredicatePlanningResult = SourcePredicatePlanner.ExpandWithPredicateMovements(
            sourcePredicatePlanningResult,
            sources,
            predicateMovementPlanningResult.Plans);
        decisions.AddRange(sourcePredicatePlanningResult.Decisions);
        pushedPredicates = sourcePredicatePlanningResult.PushedPredicatesBySourceId;
        sources = CreateSourceProperties(context, scans, requiredColumnsByAlias, pushedPredicates, decisions);
        var projectedColumns = sources.Values
            .Where(static source => source.QueryRowProjection.State == SourceProjectionState.Exact ||
                                    source.ProjectedColumns.Length > 0)
            .ToDictionary(static source => source.SourceContextId, static source => source.ProjectedColumns, StringComparer.Ordinal);
        var projectedSchemaColumns = CreateProjectedSchemaColumns(sources);
        var requiredColumnMappingPlans = CreateRequiredColumnMappingPlans(sources);
        decisions.AddRange(requiredColumnMappingPlans.Select(CreateRequiredColumnMappingDecision));
        var sourceInteractionPlanningResult = SourceInteractionPlanner.Plan(
            context,
            scans,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        decisions.AddRange(sourceInteractionPlanningResult.Decisions);
        var sourcePlanningResult = SourcePlanningPlanner.Plan(
            context,
            scans,
            requiredColumnUsageResult.UsagesBySourceId,
            sourcePredicatePlanningResult.PlansBySourceId);
        decisions.AddRange(sourcePlanningResult.Decisions);

        var facts = new PlanningFacts(
            new SourcePlanningFacts(
                sources,
                pushedPredicates,
                projectedColumns,
                projectedSchemaColumns,
                sourcePredicatePlanningResult.PlansBySourceId,
                sourceInteractionPlanningResult.PlansBySourceId,
                sourcePlanningResult.RequestsBySourceId,
                sourcePlanningResult.ResultsBySourceId,
                sourcePlanningResult.DescriptorsBySourceId,
                sourceInteractionPlanningResult.BoundaryPlans,
                sourceInteractionPlanningResult.BoundaryStrategyPlans,
                new Dictionary<string, SourceContractDiagnosticLocationMap>(StringComparer.Ordinal)),
            new RequiredColumnFacts(
                requiredColumnsByAlias,
                requiredColumnUsageResult.UsagesBySourceId,
                requiredColumnMappingPlans,
                []),
            new PhysicalStrategyFacts(
                predicatePlacementPlanningResult.Plans,
                predicateMovementPlanningResult.Plans),
            new BoundaryPruningFacts(
                [],
                []),
            new CardinalityPlanningFacts([]));
        var factsWithLocations = SourceContractDiagnosticLocationPlanner
            .WithLocations(facts.ToPlanProperties(), context, scans)
            .ToFacts();

        decisions.Add(new PlanningDecision(
            PlanningDecisionCategory.PlanProperties,
            "DeriveProperties",
            "logical-plan",
            "Derived",
            PlanningConfidence.High,
            $"Derived properties for {sources.Count} source scan(s)."));

        return new PlanningPropertyResult(factsWithLocations, decisions);
    }

    private static List<SchemaScanNode> CollectSchemaScans(LogicalNode node)
    {
        var scans = new List<SchemaScanNode>();
        AddSchemaScans(node, scans);
        return scans;
    }

    private static void AddSchemaScans(LogicalNode node, List<SchemaScanNode> scans)
    {
        if (node is SchemaScanNode scan)
            scans.Add(scan);

        foreach (var child in node.Children)
            AddSchemaScans(child, scans);
    }

    private static string FormatSource(SchemaScanNode scan)
    {
        return string.IsNullOrWhiteSpace(scan.SourceContextId)
            ? scan.Alias
            : scan.SourceContextId;
    }
}
