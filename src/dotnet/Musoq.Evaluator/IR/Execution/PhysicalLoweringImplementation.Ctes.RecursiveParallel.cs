using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool CanUseParallelLevelsWithRecursiveDefinitions(
        IReadOnlyList<ParallelCteLevel>? levels)
    {
        return levels != null && levels.All(static level =>
            level.Definitions.Count == 1 ||
            level.Definitions.All(static definition => definition.Plan is not PhysicalRecursiveCteNode));
    }

    private TableBuildResult BuildSingletonCteLevelDefinition(
        PhysicalCteNode cte,
        PhysicalCteDefinition definition,
        int index,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        CteDefinitionPruningPlan pruningPlan,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        LoweringScope scope,
        out bool storeRows,
        out LoweringScope updatedScope)
    {
        updatedScope = scope;
        if (definition.Plan is PhysicalRecursiveCteNode recursive)
        {
            recursive = ApplyRecursiveCteDefinitionPruning(definition.Name, recursive, pruningPlan);
            storeRows = true;
            var recursiveResult = BuildRecursiveCteDefinitionTable(
                cte,
                recursive,
                index,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                scope);
            return recursiveResult;
        }

        var result = BuildCteDefinitionTable(
            definition,
            index,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            pruningPlan,
            scope);
        result = ApplyCteSidecarOptimizations(
            definition.Name,
            ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name),
            cteReferenceClassifications,
            pruningPlan,
            result,
            scope,
            out var storage,
            out var sidecarUpdatedScope);
        scope = sidecarUpdatedScope;
        storeRows = storage.StoreRows;
        updatedScope = scope;
        return result;
    }
}
