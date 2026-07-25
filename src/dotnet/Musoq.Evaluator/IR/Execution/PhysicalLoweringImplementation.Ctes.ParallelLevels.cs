using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildParallelCteLevel(
        PhysicalCteNode cte,
        ParallelCteLevel level,
        string identifier,
        IReadOnlyCollection<string> cteDefinitionNames,
        Dictionary<string, int> cteIndexes,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        CteDefinitionPruningPlan pruningPlan,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        LoweringScope scope,
        out LoweringScope updatedScope)
    {
        updatedScope = scope;
        var shapes = new List<RowShape>();
        var tasks = new List<ExecutionParallelTask>();
        var mergeNodes = new List<ExecutionNode>();

        for (var taskIndex = 0; taskIndex < level.Definitions.Count; taskIndex++)
        {
            var definition = level.Definitions[taskIndex];
            var cteIndex = cteIndexes[definition.Name];
            var result = BuildCteDefinitionTable(
                definition,
                cteIndex,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes[definition.Name],
                pruningPlan,
                scope);
            var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
            result = ApplyCteSidecarOptimizations(
                definition.Name,
                sidecarSpecs,
                cteReferenceClassifications,
                pruningPlan,
                result,
                scope,
                out var storage,
                out var sidecarUpdatedScope);
            scope = sidecarUpdatedScope;

            if (!result.IsBuilt)
                return result;

            var usesTypedRowResult = storage.StoreRows &&
                                     StoredTableRowBufferEligibility.CanUseTypedRowBuffer(
                                         result.Nodes,
                                         result.Table,
                                         result.RowShape);
            var output = new ExecutionVariable(
                CreateIdentifierCandidate(
                    $"__parallelCteLevel{level.Level.ToString(CultureInfo.InvariantCulture)}Task{taskIndex.ToString(CultureInfo.InvariantCulture)}Result",
                    0),
                sidecarSpecs.Count > 0 || usesTypedRowResult ? typeof(object) : typeof(Table),
                usesTypedRowResult ? $"List<{result.RowShape.TypeName}>" : null);
            var taskNodes = new List<ExecutionNode>(result.Nodes);
            if (storage.StoreRows)
                taskNodes.Add(new ExecutionAssign(output, new ExecutionVariableRead(result.Table)));
            var taskBody = new ExecutionBlock(taskNodes);

            cteShapesByName[definition.Name] = result.RowShape;
            shapes.AddRange(result.Shapes);
            tasks.Add(new ExecutionParallelTask(
                definition.Name,
                output,
                taskBody,
                usesTypedRowResult ? cteIndex : null,
                sidecarSpecs.Count > 0 ? CreateRelatedCtePhaseQueryIdentifier(identifier, cteIndex) : null));
            if (storage.StoreRows)
                mergeNodes.Add(new ExecutionStoreTable(output, cteIndex));
        }

        var parallelBlock = new ExecutionParallelBlock(
            $"cte-level-{level.Level.ToString(CultureInfo.InvariantCulture)}",
            ResolveMaxDegreeOfParallelism(level.Definitions.Count),
            tasks,
            new ExecutionParallelMerge(new ExecutionBlock(mergeNodes)));

        updatedScope = scope;
        return TableBuildResult.Success(
            shapes,
            [parallelBlock],
            tasks[^1].Output,
            cteShapesByName[level.Definitions[^1].Name]);
    }
}
