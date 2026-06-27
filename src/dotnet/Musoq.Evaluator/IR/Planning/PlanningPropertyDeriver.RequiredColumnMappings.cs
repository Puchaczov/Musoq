using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PlanningPropertyDeriver
{
    private static RequiredColumnMappingPlan[] CreateRequiredColumnMappingPlans(
        IReadOnlyDictionary<string, SourcePlanProperties> sources)
    {
        return sources.Values
            .OrderBy(static source => source.SourceContextId, StringComparer.Ordinal)
            .Select(CreateRequiredColumnMappingPlan)
            .ToArray();
    }

    private static RequiredColumnMappingPlan CreateRequiredColumnMappingPlan(SourcePlanProperties source)
    {
        var requiredColumns = source.RequiredColumns
            .OrderBy(static column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var retainedColumns = source.ProjectedColumns.Length > 0
            ? source.ProjectedColumns.OrderBy(static column => column, StringComparer.OrdinalIgnoreCase).ToArray()
            : requiredColumns;
        var blockedColumns = source.ProjectedColumns.Length == 0 &&
                             requiredColumns.Length > 0 &&
                             !string.Equals(source.ShapeReason, "All known source columns are required.", StringComparison.Ordinal)
            ? requiredColumns
            : [];
        var mappings = retainedColumns
            .Select(column => $"{source.Alias}.{column}->{column}")
            .ToArray();
        var confidence = blockedColumns.Length > 0
            ? PlanningConfidence.Medium
            : source.ShapeConfidence;
        var reason = blockedColumns.Length > 0
            ? $"Required columns were retained but cannot yet be represented as a narrower source projection: {source.ShapeReason}"
            : $"Required columns have stable source-output mappings for alias {source.Alias}.";

        return new RequiredColumnMappingPlan(
            source.SourceContextId,
            source.Alias,
            requiredColumns,
            retainedColumns,
            blockedColumns,
            mappings,
            confidence,
            reason);
    }

    private static PlanningDecision CreateRequiredColumnMappingDecision(RequiredColumnMappingPlan plan)
    {
        var outcome = plan.RequiredColumns.Length == 0
            ? "NoRequiredColumns"
            : plan.BlockedColumns.Length == 0
                ? "Mapped"
                : "RetainedConservatively";

        return new PlanningDecision(
            PlanningDecisionCategory.RequiredColumns,
            "RequiredColumnMapping",
            plan.SourceContextId,
            outcome,
            plan.Confidence,
            plan.Reason);
    }
}
