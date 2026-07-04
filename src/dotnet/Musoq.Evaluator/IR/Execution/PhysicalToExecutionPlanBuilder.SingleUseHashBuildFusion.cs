using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private Dictionary<string, FusedCteHashBuildSource> TryPlanFusedCteHashBuildSources(
        PhysicalCteNode cte,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes)
    {
        var lowerer = new SingleUseHashBuildFusionLowerer(
            ClassifyCteReferences,
            CanFuseReadOnceCte,
            TryFindHashBuildCteRef,
            TryCreateFusedCteHashBuildSource);

        return lowerer.TryPlanFusedSources(
            cte,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes);
    }

    private static IReadOnlyDictionary<string, FusedCteHashBuildSource>? MergeFusedCteHashBuildSources(
        IReadOnlyDictionary<string, FusedCteHashBuildSource>? previous,
        IReadOnlyDictionary<string, FusedCteHashBuildSource> current)
    {
        if (current.Count == 0)
            return previous;

        if (previous == null || previous.Count == 0)
            return current;

        var merged = new Dictionary<string, FusedCteHashBuildSource>(previous, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, source) in current)
            merged[name] = source;

        return merged;
    }
}
