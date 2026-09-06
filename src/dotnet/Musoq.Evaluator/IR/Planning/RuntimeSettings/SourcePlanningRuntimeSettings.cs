using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal static partial class SourcePlanningPlanner
{
    private static SourcePlanRequest CreateEmptyRequest(
        PlanningContext context,
        SourceIdentity identity,
        SchemaScanNode scan,
        IReadOnlyDictionary<string, RequiredColumnUsage[]> requiredColumnUsagesBySourceId,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlansBySourceId)
    {
        return new SourcePlanRequest
        {
            Identity = identity,
            SourceRuntimeSettings = ResolveSourceRuntimeSettings(context, scan),
            RequiredColumns = ResolveRequiredColumns(context, scan, requiredColumnUsagesBySourceId),
            Predicate = ResolvePredicate(scan, sourcePredicatePlansBySourceId, context.CancellationToken)
        };
    }

    private static IReadOnlyDictionary<string, string> ResolveSourceRuntimeSettings(
        PlanningContext context,
        SchemaScanNode scan)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var sourceNode = ResolveSourceNode(context, scan);
        if (sourceNode != null &&
            context.SourcePlanRequestsBySource.TryGetValue(sourceNode, out var sourceRequest))
        {
            return sourceRequest.SourceRuntimeSettings;
        }

        foreach (var entry in context.SourcePlanRequestsBySource)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(entry.Key.Id, scan.SourceContextId, StringComparison.Ordinal))
                return entry.Value.SourceRuntimeSettings;
        }

        return new Dictionary<string, string>();
    }
}
