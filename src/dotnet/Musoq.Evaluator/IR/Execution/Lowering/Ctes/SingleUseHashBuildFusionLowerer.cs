using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed class SingleUseHashBuildFusionLowerer
{
    public delegate bool TryFindHashBuildCteRef(
        PhysicalNode query,
        string cteName,
        out PhysicalCteRefNode cteRef);

    public delegate bool TryCreateFusedCteHashBuildSource(
        PhysicalCteDefinition definition,
        int definitionIndex,
        PhysicalCteRefNode cteRef,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope,
        out FusedCteHashBuildSource fusion);

    private readonly Func<PhysicalCteNode, IReadOnlyDictionary<string, CteReferenceClassification>> _classifyCteReferences;
    private readonly Func<string, IReadOnlyDictionary<string, CteReferenceClassification>, bool> _canFuseReadOnceCte;
    private readonly TryFindHashBuildCteRef _tryFindHashBuildCteRef;
    private readonly TryCreateFusedCteHashBuildSource _tryCreateFusedSource;

    public SingleUseHashBuildFusionLowerer(
        Func<PhysicalCteNode, IReadOnlyDictionary<string, CteReferenceClassification>> classifyCteReferences,
        Func<string, IReadOnlyDictionary<string, CteReferenceClassification>, bool> canFuseReadOnceCte,
        TryFindHashBuildCteRef tryFindHashBuildCteRef,
        TryCreateFusedCteHashBuildSource tryCreateFusedSource)
    {
        _classifyCteReferences = classifyCteReferences;
        _canFuseReadOnceCte = canFuseReadOnceCte;
        _tryFindHashBuildCteRef = tryFindHashBuildCteRef;
        _tryCreateFusedSource = tryCreateFusedSource;
    }

    public Dictionary<string, FusedCteHashBuildSource> TryPlanFusedSources(
        PhysicalCteNode cte,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        IReadOnlyDictionary<string, int> schemaFromIndexes,
        LoweringScope scope)
    {
        var classifications = _classifyCteReferences(cte);
        var fusions = new Dictionary<string, FusedCteHashBuildSource>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < cte.Definitions.Length; index++)
        {
            var definition = cte.Definitions[index];
            if (!CanFuseGeneratedSubqueryHashBuild(definition.Name) ||
                !_canFuseReadOnceCte(definition.Name, classifications) ||
                !_tryFindHashBuildCteRef(cte.Query, definition.Name, out var cteRef) ||
                !_tryCreateFusedSource(
                    definition,
                    index,
                    cteRef,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes[definition.Name],
                    scope,
                    out var fusion))
            {
                continue;
            }

            fusions.Add(definition.Name, fusion);
        }

        return fusions;
    }

    private static bool CanFuseGeneratedSubqueryHashBuild(string definitionName)
    {
        return GeneratedSubqueryContract.IsGeneratedSubqueryCteName(definitionName);
    }
}
