using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildTable(
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        if (pipeline.Source is PhysicalUnpivotNode unpivot)
            return BuildUnpivotTable(unpivot, pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, scope);

        if (pipeline.Source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildJoinTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, scope);

        if (pipeline.Source is PhysicalNestedLoopApplyNode apply)
        {
            var applyAttempt = _applyLoweringService.BuildApplyTable(
                apply,
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                inheritedSourceLookup: null,
                scope: scope);
            return applyAttempt.Kind switch
            {
                LoweringAttemptKind.Built => applyAttempt.RequireValue().ToCompatibilityResult(),
                LoweringAttemptKind.Unsupported => TableBuildResult.Unsupported(
                    applyAttempt.RequireUnsupportedReason()),
                _ => TableBuildResult.Unsupported("Execution IR apply lowering did not claim the source node.")
            };
        }

        var sourceShape = ResolveSourceShape(pipeline.Source, cteIndexes, cteShapesByName);
        if (sourceShape == null)
            return TableBuildResult.Unsupported($"Execution IR lowering cannot resolve source shape for {pipeline.Source.GetType().Name}.");

        if (TryBuildTypedOrderTable(
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                sourceShape,
                schemaFromIndex,
                scope,
                out var typedOrderResult))
        {
            return typedOrderResult;
        }

        var sourceRowsScope = CreateSourceRowsScope(resultTableName);
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            RowShapeLookup.CreateSourceShapeLookup(sourceShape));
        if (!projection.IsBuilt)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultShape = postOperationProjection.WorkingShape;
        var source = CreateSourceVariable(pipeline.Source, sourceShape, cteShapesByName);
        var sourceSetup = CreateSourceSetup(pipeline.Source, sourceShape, source, schemaFromIndex, cteIndexes, sourceRowsScope, cteShapesByName);
        var sourceRows = CreateSourceRowsExpression(
            pipeline.Source,
            sourceShape,
            cteIndexes,
            cteShapesByName,
            sourceRowsScope,
            scope);
        var resultTable = postOperationProjection.WorkingTable;
        var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, sourceShape);
        var streamingSlice = TryCreateStreamingSlice(
            resultTable.Name,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection,
            pipeline.Project.Fields,
            out var remainingPostOperations);
        var outputAppend = CreateOutputAppend(appendRow, scope);
        var loopBody = scope.DirectTableSink == null
            ? CreateLoopBody(pipeline.Filter, appendRow, sourceShape, streamingSlice)
            : CreateLoopBody(pipeline.Filter, outputAppend, sourceShape);
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, loopBody);
        var loopNode = scope.DirectTableSink == null
            ? TryCreateParallelFilterProjectLoop(
                  pipeline,
                  sourceShape,
                  source,
                  sourceRows,
                  loop,
                  appendRow)
              ?? (ExecutionNode)loop
            : loop;
        var nodes = new List<ExecutionNode>(sourceSetup.Count + pipeline.PostOperations.Count + 2);

        nodes.AddRange(sourceSetup);
        AddOutputTableCreation(
            nodes,
            resultTable,
            resultShape,
            scope,
            CreateStreamingSliceCapacityCandidate(resultTable, streamingSlice));
        nodes.AddRange(CreateStreamingSliceCounterDeclarations(streamingSlice));
        nodes.Add(loopNode);

        return CompleteOutputTableBuild(
            scope,
            [sourceShape, ..postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            remainingPostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection);
    }
}
