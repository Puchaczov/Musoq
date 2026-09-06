using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.SourcePlanning;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PlanningPropertyDeriver
{
    public static PlanningPropertyResult Derive(PlanningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();
        var decisions = new List<PlanningDecision>();
        var scans = CollectSchemaScans(context.LogicalPlan, context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        var requiredColumnUsageResult = RequiredColumnUsagePlanner.Plan(
            context.LogicalPlan,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(requiredColumnUsageResult.Decisions);
        var requiredColumnsByAlias = requiredColumnUsageResult.RequiredColumnsByAlias;
        var sourcePredicatePlanningResult = SourcePredicatePlanner.Plan(
            context.UsedWhereNodes,
            context.InferredColumns);
        context.CancellationToken.ThrowIfCancellationRequested();
        var pushedPredicates = sourcePredicatePlanningResult.PushedPredicatesBySourceId;
        var preliminaryDecisions = new List<PlanningDecision>();
        var sources = CreateSourceProperties(context, scans, requiredColumnsByAlias, pushedPredicates, preliminaryDecisions);
        context.CancellationToken.ThrowIfCancellationRequested();
        var sourceInteractionForMovement = SourceInteractionPlanner.Plan(
            context,
            scans,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        var predicatePlacementPlanningResult = PredicatePlacementPlanner.Plan(
            context.LogicalPlan,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(predicatePlacementPlanningResult.Decisions);
        var predicateMovementPlanningResult = PredicateMovementPlanner.Plan(
            context.LogicalPlan,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId,
            sourceInteractionForMovement.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(predicateMovementPlanningResult.Decisions);

        sourcePredicatePlanningResult = SourcePredicatePlanner.ExpandWithPredicateMovements(
            sourcePredicatePlanningResult,
            sources,
            predicateMovementPlanningResult.Plans);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(sourcePredicatePlanningResult.Decisions);
        pushedPredicates = sourcePredicatePlanningResult.PushedPredicatesBySourceId;
        sources = CreateSourceProperties(context, scans, requiredColumnsByAlias, pushedPredicates, decisions);
        context.CancellationToken.ThrowIfCancellationRequested();
        var projectedColumns = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var source in sources.Values)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (source.QueryRowProjection.State == SourceProjectionState.Exact ||
                source.ProjectedColumns.Length > 0)
                projectedColumns[source.SourceContextId] = source.ProjectedColumns;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        var projectedSchemaColumns = CreateProjectedSchemaColumns(sources, context.CancellationToken);
        var requiredColumnMappingPlans = CreateRequiredColumnMappingPlans(sources, context.CancellationToken);
        foreach (var requiredColumnMappingPlan in requiredColumnMappingPlans)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            decisions.Add(CreateRequiredColumnMappingDecision(requiredColumnMappingPlan));
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        var sourceInteractionPlanningResult = SourceInteractionPlanner.Plan(
            context,
            scans,
            sources,
            sourcePredicatePlanningResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(sourceInteractionPlanningResult.Decisions);
        var sourcePlanningResult = SourcePlanningPlanner.Plan(
            context,
            scans,
            requiredColumnUsageResult.UsagesBySourceId,
            sourcePredicatePlanningResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(sourcePlanningResult.Decisions);
        var applyPredicateMovementPlanningResult = ApplyPredicateMovementPlanner.Plan(
            context.LogicalPlan,
            sources,
            sourcePlanningResult.ResultsBySourceId,
            sourcePredicatePlanningResult.PlansBySourceId);
        context.CancellationToken.ThrowIfCancellationRequested();
        decisions.AddRange(applyPredicateMovementPlanningResult.Decisions);

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
                predicateMovementPlanningResult.Plans,
                applyPredicateMovementPlanningResult.Plans),
            new BoundaryPruningFacts(
                [],
                []),
            new CardinalityPlanningFacts([]));
        var factsWithLocations = SourceContractDiagnosticLocationPlanner
            .WithLocations(facts.ToPlanProperties(), context, scans)
            .ToFacts();
        context.CancellationToken.ThrowIfCancellationRequested();

        decisions.Add(new PlanningDecision(
            PlanningDecisionCategory.PlanProperties,
            "DeriveProperties",
            "logical-plan",
            "Derived",
            PlanningConfidence.High,
            $"Derived properties for {sources.Count} source scan(s)."));

        context.CancellationToken.ThrowIfCancellationRequested();
        return new PlanningPropertyResult(factsWithLocations, decisions);
    }

    private static List<SchemaScanNode> CollectSchemaScans(
        LogicalNode node,
        CancellationToken cancellationToken)
    {
        var scans = new List<SchemaScanNode>();
        AddSchemaScans(node, scans, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return scans;
    }

    private static void AddSchemaScans(
        LogicalNode node,
        List<SchemaScanNode> scans,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node is SchemaScanNode scan)
            scans.Add(scan);

        foreach (var child in node.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddSchemaScans(child, scans, cancellationToken);
        }
    }

    private static string FormatSource(SchemaScanNode scan)
    {
        return string.IsNullOrWhiteSpace(scan.SourceContextId)
            ? scan.Alias
            : scan.SourceContextId;
    }
}
