using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record FusedCteHashBuildSource(
        GeneratedRowShape RowShape,
        IReadOnlyList<RowShape> DefinitionShapes,
        RowShape ProducerShape,
        ExecutionVariable ProducerVariable,
        IReadOnlyList<ExecutionNode> ProducerSetup,
        ExecutionExpression ProducerRows,
        IReadOnlyList<ExecutionRowValue> RowValues,
        IReadOnlyList<ExecutionExpression> ContextValues,
        ExecutionContextLayout? ContextLayout,
        int SchemaSourceCount,
        HashPayloadShape? HashPayloadShape);

    private Dictionary<string, FusedCteHashBuildSource> TryPlanFusedCteHashBuildSources(
        PhysicalCteNode cte,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes)
    {
        var classifications = ClassifyCteReferences(cte);
        var fusions = new Dictionary<string, FusedCteHashBuildSource>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < cte.Definitions.Length; index++)
        {
            var definition = cte.Definitions[index];
            if (!CanFuseGeneratedSubqueryHashBuild(definition.Name) ||
                !CanFuseReadOnceCte(definition.Name, classifications) ||
                !TryFindHashBuildCteRef(cte.Query, definition.Name, out var cteRef) ||
                !TryCreateFusedCteHashBuildSource(
                    definition,
                    index,
                    cteRef,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes[definition.Name],
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
