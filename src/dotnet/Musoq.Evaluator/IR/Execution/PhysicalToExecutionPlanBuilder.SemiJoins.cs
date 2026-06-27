using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildSemiHashJoinTable(HashJoinBuildContext context, bool isAntiSemiJoin)
    {
        if (CanUsePayloadFreeSemiJoinKeySet(context))
            return BuildSemiKeySetJoinTable(context, isAntiSemiJoin);

        var outputLookup = RowShapeLookup.CreateSourceShapeLookup(context.Sides.Probe.Shape);
        var resultShape = CreateGeneratedShape(context.ResultShapeName, context.Pipeline.Project.Fields, outputLookup);
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, context.Pipeline.Project.Fields, outputLookup);
        var hasMatch = isAntiSemiJoin
            ? new ExecutionVariable($"{context.Hash.Name}HasMatch", typeof(bool))
            : null;
        var matchedBody = isAntiSemiJoin
            ? CreateSemiJoinMarkMatchedBody(context.Join.Residual, hasMatch!, context.ConversionLookup)
            : CreateSemiJoinAppendMatchedBody(
                context.Join.Residual,
                context.Pipeline.Filter,
                appendRow,
                context.ConversionLookup,
                outputLookup);
        var noMatchBody = isAntiSemiJoin
            ? CreateLoopBody(context.Pipeline.Filter, appendRow, outputLookup)
            : null;
        return CompleteHashJoinTableBuild(
            context,
            new HashJoinTableLowering(resultTable, resultShape, matchedBody, noMatchBody, hasMatch));
    }

    private TableBuildResult BuildSemiNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        JoinSources joinSources,
        bool isAntiSemiJoin)
    {
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var outputLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape);
        var conversionLookup = RowShapeLookup.CreateTransitionAliasLookup(sourceLookup);
        var resultShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, outputLookup);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, pipeline.Project.Fields, outputLookup);
        var hasMatch = isAntiSemiJoin
            ? new ExecutionVariable($"{resultTableName}HasMatch", typeof(bool))
            : null;
        var matchedBody = isAntiSemiJoin
            ? CreateSemiJoinMarkMatchedBody(join.OnPredicate, hasMatch!, conversionLookup)
            : CreateSemiJoinAppendMatchedBody(join.OnPredicate, pipeline.Filter, appendRow, conversionLookup, outputLookup);
        var nodes = CreateJoinPrelude(joinSources, resultTable, resultShape);
        var rightRows = CreateNestedLoopInnerRows(joinSources.Right, nodes);
        var rightLoop = CreateSourceLoop(joinSources.Right.Shape, rightRows, joinSources.Right.Variable, matchedBody);
        var leftBody = isAntiSemiJoin
            ? CreateAntiSemiNestedLoopBody(hasMatch!, rightLoop, pipeline.Filter, appendRow, outputLookup)
            : new ExecutionBlock([rightLoop]);
        var leftLoop = CreateSourceLoop(joinSources.Left.Shape, joinSources.Left.Rows, joinSources.Left.Variable, leftBody);

        nodes.Add(leftLoop);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, resultShape],
            nodes,
            resultTable,
            resultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

    private ExecutionBlock CreateSemiJoinAppendMatchedBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> joinLookup,
        IReadOnlyDictionary<string, RowShape> outputLookup)
    {
        var appendBlock = CreateLoopBody(filter, appendRow, outputLookup);
        var matchedBlock = new ExecutionBlock([..appendBlock.Nodes, new ExecutionBreak()]);

        return CreateConditionalJoinBlock(joinCondition, joinLookup, matchedBlock);
    }

    private static ExecutionBlock CreateSemiJoinMarkMatchedBody(
        IrExpression? joinCondition,
        ExecutionVariable hasMatch,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var matchedBlock = new ExecutionBlock(
        [
            new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool))),
            new ExecutionBreak()
        ]);

        return CreateConditionalJoinBlock(joinCondition, sourceLookup, matchedBlock);
    }

    private ExecutionBlock CreateAntiSemiNestedLoopBody(
        ExecutionVariable hasMatch,
        ExecutionNode rightLoop,
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> outputLookup)
    {
        var unmatchedAppend = CreateLoopBody(filter, appendRow, outputLookup);

        return new ExecutionBlock(
        [
            new ExecutionLet(hasMatch, new ExecutionLiteral(false, typeof(bool))),
            rightLoop,
            new ExecutionIf(
                new ExecutionUnary(
                    UnaryOpKind.Not,
                    new ExecutionVariableRead(hasMatch),
                    typeof(bool)),
                unmatchedAppend)
        ]);
    }
}
