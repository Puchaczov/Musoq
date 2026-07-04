using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildTable(
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        PhysicalToExecutionLoweringSession? session = null)
    {
        session ??= new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        if (pipeline.Source is PhysicalUnpivotNode unpivot)
            return BuildUnpivotTable(unpivot, pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, session);

        if (pipeline.Source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildJoinTable(pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, session);

        if (pipeline.Source is PhysicalNestedLoopApplyNode apply)
            return BuildApplyTable(apply, pipeline, resultTableName, resultShapeName, cteIndexes, cteShapesByName, schemaFromIndex, session: session);

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
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultShape = postOperationProjection.WorkingShape;
        var source = CreateSourceVariable(pipeline.Source, sourceShape, cteShapesByName);
        var sourceSetup = CreateSourceSetup(pipeline.Source, sourceShape, source, schemaFromIndex, cteIndexes, sourceRowsScope, cteShapesByName);
        var sourceRows = CreateSourceRowsExpression(pipeline.Source, sourceShape, cteIndexes, cteShapesByName, sourceRowsScope);
        var resultTable = postOperationProjection.WorkingTable;
        var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, sourceShape);
        var streamingSlice = TryCreateStreamingSlice(
            resultTable.Name,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection,
            pipeline.Project.Fields,
            out var remainingPostOperations);
        var loopBody = CreateLoopBody(pipeline.Filter, appendRow, sourceShape, streamingSlice);
        var loop = CreateSourceLoop(sourceShape, sourceRows, source, loopBody);
        var loopNode = TryCreateParallelFilterProjectLoop(
            pipeline,
            sourceShape,
            source,
            sourceRows,
            loop,
            appendRow)
            ?? (ExecutionNode)loop;
        var nodes = new List<ExecutionNode>(sourceSetup.Count + pipeline.PostOperations.Count + 2);

        nodes.AddRange(sourceSetup);
        nodes.Add(CreateTable(resultTable, resultShape, CreateStreamingSliceCapacityCandidate(resultTable, streamingSlice)));
        nodes.AddRange(CreateStreamingSliceCounterDeclarations(streamingSlice));
        nodes.Add(loopNode);

        return CompleteTableBuild(
            [sourceShape, ..postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            remainingPostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection);
    }
}
