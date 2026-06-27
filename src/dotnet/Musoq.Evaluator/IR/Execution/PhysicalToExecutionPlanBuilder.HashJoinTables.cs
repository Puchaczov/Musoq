using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildInnerHashJoinTable(HashJoinBuildContext context)
    {
        var resultShape = CreateGeneratedShape(context.ResultShapeName, context.Pipeline.Project.Fields, context.SourceLookup);
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, context.Pipeline.Project.Fields, context.SourceLookup);
        var matchedBody = CreateJoinLoopBody(context.Join.Residual, context.Pipeline.Filter, appendRow, context.ConversionLookup);
        return CompleteHashJoinTableBuild(context, new HashJoinTableLowering(resultTable, resultShape, matchedBody));
    }

    private TableBuildResult BuildOuterHashJoinTable(HashJoinBuildContext context)
    {
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var nullAlias = RowShapeLookup.ResolveSourceAlias(context.Sides.Build.Shape);
        var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
            context.ResultShapeName,
            resultTable,
            context.Pipeline.Project.Fields,
            context.SourceLookup,
            nullAlias));
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var appendBlocks = CreateOuterApplyAppendBlocks(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            projection.UnmatchedAppendRow,
            context.SourceLookup,
            nullAlias);
        if (!appendBlocks.Supported)
            return TableBuildResult.Unsupported(appendBlocks.UnsupportedReason);

        var resultShape = projection.ResultShape;
        var hasMatch = new ExecutionVariable($"{context.Hash.Name}HasMatch", typeof(bool));
        var matchedBody = CreateOuterHashJoinMatchedBody(
            context.Join.Residual,
            context.Pipeline.Filter,
            hasMatch,
            projection.MatchedAppendRow,
            context.ConversionLookup);
        return CompleteHashJoinTableBuild(
            context,
            new HashJoinTableLowering(
                resultTable,
                resultShape,
                matchedBody,
                appendBlocks.UnmatchedAppendBlock,
                hasMatch));
    }

    private ExecutionBlock CreateOuterHashJoinMatchedBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionVariable hasMatch,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var appendBlock = CreateOuterJoinMatchedAppendBlock(filter, appendRow, sourceLookup);
        var matchedBlock = new ExecutionBlock(
        [
            new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool))),
            ..appendBlock.Nodes
        ]);

        return CreateConditionalJoinBlock(joinCondition, sourceLookup, matchedBlock);
    }
}
