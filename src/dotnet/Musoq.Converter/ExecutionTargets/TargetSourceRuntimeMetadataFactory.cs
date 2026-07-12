using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Build;

internal static class TargetSourceRuntimeMetadataFactory
{
    public static IReadOnlyList<TargetSourceRuntimeMetadata> Create(
        SemanticBuildArtifacts semantic,
        PlanningBuildArtifacts planning)
    {
        var plannedSources = planning.PlanningResult?.Properties.SourcePlanResultsBySourceId ??
                             new Dictionary<string, SourcePlanResult>(StringComparer.Ordinal);
        var sourceContextIds = plannedSources.Keys
            .Concat(semantic.SourceRuntimeSettingDescriptionsBySourceContextId.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static sourceContextId => sourceContextId, StringComparer.Ordinal);

        return sourceContextIds
            .Select(sourceContextId => new TargetSourceRuntimeMetadata(
                sourceContextId,
                plannedSources.TryGetValue(sourceContextId, out var sourcePlan)
                    ? GetAcceptedOperations(sourcePlan)
                    : [],
                semantic.SourceRuntimeSettingDescriptionsBySourceContextId.TryGetValue(sourceContextId, out var descriptions)
                    ? descriptions.Select(static description => new TargetRuntimeSettingAbiContract(
                        description.Name,
                        description.Required,
                        description.Phases.ToString(),
                        description.Status.ToString(),
                        description.Secret ? string.Empty : description.Description))
                    : []))
            .ToArray();
    }

    private static IReadOnlyList<TargetSourcePlanOperation> GetAcceptedOperations(SourcePlanResult sourcePlan)
    {
        var operations = new List<TargetSourcePlanOperation>();
        if (sourcePlan.AcceptedColumns.Count > 0)
            operations.Add(TargetSourcePlanOperation.Columns);
        if (sourcePlan.AcceptedPredicate != null)
            operations.Add(TargetSourcePlanOperation.Predicate);
        if (sourcePlan.AcceptedOrderBy.Count > 0)
            operations.Add(TargetSourcePlanOperation.OrderBy);
        if (sourcePlan.AcceptedSkip.HasValue)
            operations.Add(TargetSourcePlanOperation.Skip);
        if (sourcePlan.AcceptedTake.HasValue)
            operations.Add(TargetSourcePlanOperation.Take);
        return operations;
    }
}
