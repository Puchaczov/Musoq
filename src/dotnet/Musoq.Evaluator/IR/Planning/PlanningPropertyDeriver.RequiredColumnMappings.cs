using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PlanningPropertyDeriver
{
    private static RequiredColumnMappingPlan[] CreateRequiredColumnMappingPlans(
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var orderedSources = sources.Values.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        orderedSources.Sort((left, right) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return StringComparer.Ordinal.Compare(left.SourceContextId, right.SourceContextId);
        });

        var plans = new RequiredColumnMappingPlan[orderedSources.Count];
        for (var index = 0; index < orderedSources.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            plans[index] = CreateRequiredColumnMappingPlan(orderedSources[index], cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return plans;
    }

    private static RequiredColumnMappingPlan CreateRequiredColumnMappingPlan(
        SourcePlanProperties source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requiredColumns = source.RequiredColumns
            .OrderBy(static column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        var retainedColumns = source.ProjectedColumns.Length > 0
            ? source.ProjectedColumns.OrderBy(static column => column, StringComparer.OrdinalIgnoreCase).ToArray()
            : requiredColumns;
        cancellationToken.ThrowIfCancellationRequested();
        var blockedColumns = source.ProjectedColumns.Length == 0 &&
                             requiredColumns.Length > 0 &&
                             !string.Equals(source.ShapeReason, "All known source columns are required.", StringComparison.Ordinal)
            ? requiredColumns
            : [];
        var mappings = new string[retainedColumns.Length];
        for (var index = 0; index < retainedColumns.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            mappings[index] = $"{source.Alias}.{retainedColumns[index]}->{retainedColumns[index]}";
        }
        var confidence = blockedColumns.Length > 0
            ? PlanningConfidence.Medium
            : source.ShapeConfidence;
        var reason = blockedColumns.Length > 0
            ? $"Required columns were retained but cannot yet be represented as a narrower source projection: {source.ShapeReason}"
            : $"Required columns have stable source-output mappings for alias {source.Alias}.";

        cancellationToken.ThrowIfCancellationRequested();
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
