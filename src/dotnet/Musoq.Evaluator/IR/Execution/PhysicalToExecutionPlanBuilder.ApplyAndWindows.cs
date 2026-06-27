using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildApplyTable(
        PhysicalNestedLoopApplyNode apply,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape>? inheritedSourceLookup = null)
    {
        if (apply.Kind is not (ApplyKind.Cross or ApplyKind.Outer))
        {
            return TableBuildResult.Unsupported(
                $"Execution IR apply lowering currently supports only cross and outer apply. Found {apply.Kind}.");
        }

        var inheritedLookup = inheritedSourceLookup ?? new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase);
        var sourceRowsScope = CreateSourceRowsScope(resultTableName);

        if (apply.Kind == ApplyKind.Cross)
        {
            var chain = BuildCrossApplyChain(
                apply,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                inheritedLookup,
                sourceRowsScope);
            if (chain.Supported)
            {
                return BuildCrossApplyChainTable(
                    chain.Chain,
                    pipeline,
                    resultTableName,
                    resultShapeName);
            }
        }

        var leftSource = BuildApplySource(
            apply.Left,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            inheritedLookup,
            sourceRowsScope);
        if (!leftSource.Supported)
            return TableBuildResult.Unsupported(leftSource.UnsupportedReason);

        var leftLookup = RowShapeLookup.CreateSourceShapeLookup(inheritedLookup, leftSource.Source.Shape);
        var rightSource = BuildApplySource(
            apply.Right,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex + leftSource.Source.SchemaSourceCount,
            leftLookup,
            sourceRowsScope);
        if (!rightSource.Supported)
            return TableBuildResult.Unsupported(rightSource.UnsupportedReason);

        if (apply.WithOrdinality)
            rightSource = SourceBuildResult.Success(AddApplyOrdinalityAccess(rightSource.Source));

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(leftLookup, rightSource.Source.Shape);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        GeneratedRowShape resultShape;
        ExecutionNode leftLoop;
        TableProjection? finalProjection = null;
        IReadOnlyList<PostOperation> postOperations = pipeline.PostOperations;
        IReadOnlyList<GeneratedRowShape> resultShapes;

        if (apply.Kind == ApplyKind.Cross)
        {
            var projection = CreatePostOperationProjection(
                resultTableName,
                resultShapeName,
                pipeline.Project.Fields,
                pipeline.PostOperations,
                sourceLookup);
            if (!projection.Supported)
                return TableBuildResult.Unsupported(projection.UnsupportedReason);

            var postOperationProjection = projection.Value
                ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

            resultTable = postOperationProjection.WorkingTable;
            resultShape = postOperationProjection.WorkingShape;
            finalProjection = postOperationProjection.FinalProjection;
            postOperations = postOperationProjection.PostOperations;
            resultShapes = postOperationProjection.Shapes;

            var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, sourceLookup);
            var loopBody = CreateLoopBody(pipeline.Filter, appendRow, sourceLookup);
            IReadOnlyList<ExecutionNode> rightSetup = rightSource.Source.CanReuseSetupAcrossApplyRows
                ? []
                : rightSource.Source.Setup;
            var rightLoop = CreateApplySourceLoop(rightSource.Source, loopBody);
            leftLoop = CreateSourceLoop(
                leftSource.Source.Shape,
                leftSource.Source.Rows,
                leftSource.Source.Variable,
                new ExecutionBlock([..rightSetup, rightLoop]));
        }
        else
        {
            var rightAlias = RowShapeLookup.ResolveSourceAlias(rightSource.Source.Shape);
            var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
                resultShapeName,
                resultTable,
                pipeline.Project.Fields,
                sourceLookup,
                rightAlias));
            if (!projection.Supported)
                return TableBuildResult.Unsupported(projection.UnsupportedReason);

            resultShape = projection.ResultShape;
            resultShapes = [resultShape];
            var hasMatch = new ExecutionVariable($"{rightAlias}HasMatch", typeof(bool));
            IReadOnlyList<ExecutionNode> rightSetup = rightSource.Source.CanReuseSetupAcrossApplyRows
                ? []
                : rightSource.Source.Setup;
            var appendBlocks = CreateOuterApplyAppendBlocks(
                pipeline.Filter,
                projection.MatchedAppendRow,
                projection.UnmatchedAppendRow,
                sourceLookup,
                rightAlias);
            if (!appendBlocks.Supported)
                return TableBuildResult.Unsupported(appendBlocks.UnsupportedReason);

            var rightLoop = CreateApplySourceLoop(
                rightSource.Source,
                new ExecutionBlock(
                [
                    new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool))),
                    ..appendBlocks.MatchedAppendBlock.Nodes
                ]));
            leftLoop = CreateSourceLoop(
                leftSource.Source.Shape,
                leftSource.Source.Rows,
                leftSource.Source.Variable,
                new ExecutionBlock(
                [
                    ..rightSetup,
                    new ExecutionLet(hasMatch, new ExecutionLiteral(false, typeof(bool))),
                    rightLoop,
                    new ExecutionIf(
                        new ExecutionUnary(
                            UnaryOpKind.Not,
                            new ExecutionVariableRead(hasMatch),
                            typeof(bool)),
                        appendBlocks.UnmatchedAppendBlock)
                ]));
        }

        var nodes = new List<ExecutionNode>(leftSource.Source.Setup.Count + 2);

        nodes.AddRange(leftSource.Source.Setup);
        if (rightSource.Source.CanReuseSetupAcrossApplyRows)
            nodes.AddRange(rightSource.Source.Setup);

        nodes.Add(CreateTable(resultTable, resultShape));
        nodes.Add(leftLoop);

        return CompleteTableBuild(
            [..leftSource.Source.Shapes, ..rightSource.Source.Shapes, ..resultShapes],
            nodes,
            resultTable,
            resultShape,
            postOperations,
            pipeline.Project.IsDistinct,
            finalProjection);
    }

}
