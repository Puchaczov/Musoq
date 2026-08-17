using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.SourcePlanning;


using Musoq.Schema;
using Musoq.Schema.DataSources;

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
            .Where(static source => source.ProjectedColumns.Length > 0)
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

    private static Dictionary<string, ISchemaColumn[]> CreateProjectedSchemaColumns(
        IReadOnlyDictionary<string, SourcePlanProperties> sources)
    {
        var result = new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal);

        foreach (var source in sources.Values)
        {
            if (source.ProjectedColumns.Length == 0)
                continue;

            result[source.SourceContextId] = source.ProjectedSchemaColumns;
        }

        return result;
    }

    private static Dictionary<string, SourcePlanProperties> CreateSourceProperties(
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans,
        IReadOnlyDictionary<string, IReadOnlySet<string>> requiredColumnsByAlias,
        IReadOnlyDictionary<string, IrExpression[]> pushedPredicates,
        List<PlanningDecision> decisions)
    {
        var sources = new Dictionary<string, SourcePlanProperties>(StringComparer.Ordinal);

        foreach (var scan in scans)
        {
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            var requiredColumns = requiredColumnsByAlias.TryGetValue(scan.Alias, out var required)
                ? required.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                : [];
            var predicates = pushedPredicates.TryGetValue(scan.SourceContextId, out var pushed)
                ? pushed
                : [];
            var projection = TryCreateProjectedColumns(
                context,
                scan,
                requiredColumns,
                decisions,
                out var projectedSchemaColumns,
                out var confidence,
                out var reason);

            sources[scan.SourceContextId] = new SourcePlanProperties(
                scan.SourceContextId,
                scan.Alias,
                scan.SchemaName,
                scan.MethodName,
                requiredColumns,
                predicates,
                projection,
                projectedSchemaColumns,
                confidence,
                reason);
        }

        return sources;
    }

    private static string[] TryCreateProjectedColumns(
        PlanningContext context,
        SchemaScanNode scan,
        string[] requiredColumns,
        List<PlanningDecision> decisions,
        out ISchemaColumn[] projectedSchemaColumns,
        out PlanningConfidence confidence,
        out string reason)
    {
        projectedSchemaColumns = [];
        confidence = PlanningConfidence.Low;

        if (requiredColumns.Length == 0)
        {
            reason = "No required source columns were proven.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var entityType = SourceEntityMetadataResolver.ResolveSourceEntityType(context.Scope, scan.Alias);
        if (entityType == null)
        {
            reason = "Source entity type could not be resolved from metadata.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        if (SourceEntityMetadataResolver.IsDynamicEntity(entityType))
        {
            reason = $"Source entity type {entityType.Name} is dynamic.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var availableColumns = ResolveAvailableColumns(context, scan);
        if (availableColumns.Length == 0)
        {
            reason = "Source columns could not be resolved.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var availableByName = availableColumns.ToDictionary(
            static column => column.ColumnName,
            StringComparer.OrdinalIgnoreCase);

        if (requiredColumns.Any(requiredColumn => !availableByName.ContainsKey(requiredColumn)))
        {
            reason = "At least one required column was not present in source metadata.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        var requiredSet = new HashSet<string>(requiredColumns, StringComparer.OrdinalIgnoreCase);
        var projected = availableColumns
            .Where(column => requiredSet.Contains(column.ColumnName))
            .ToArray();
        var projectedColumnNames = projected
            .Select(static column => column.ColumnName)
            .ToArray();

        confidence = PlanningConfidence.High;
        if (projected.Length >= availableColumns.Length)
        {
            reason = "All known source columns are required.";
            AddProjectionDecision(decisions, scan, "Skipped", confidence, reason);
            return [];
        }

        projectedSchemaColumns = projected;
        reason = $"Retained {projected.Length} of {availableColumns.Length} known source column(s).";
        AddProjectionDecision(decisions, scan, "Applied", confidence, reason);
        return projectedColumnNames;
    }

    private static void AddProjectionDecision(
        List<PlanningDecision> decisions,
        SchemaScanNode scan,
        string outcome,
        PlanningConfidence confidence,
        string reason)
    {
        decisions.Add(new PlanningDecision(
            PlanningDecisionCategory.ProjectionPruning,
            "SourceProjection",
            FormatSource(scan),
            outcome,
            confidence,
            reason));
    }

    private static ISchemaColumn[] ResolveAvailableColumns(PlanningContext context, SchemaScanNode scan)
    {
        if (context.InferredColumns.TryGetValue(scan.Alias, out var inferredColumns) && inferredColumns.Length > 0)
            return inferredColumns;

        return scan.OutputSchema.Columns
            .Select(static column => (ISchemaColumn)new SchemaColumn(column.Name, column.Index, column.Type))
            .ToArray();
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
