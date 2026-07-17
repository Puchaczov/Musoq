using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildMarkHashJoinTable(HashJoinBuildContext context)
    {
        if (context.Join.Kind != JoinKind.LeftMark ||
            !ReferenceEquals(context.Sides.Build, context.Sources.Right))
        {
            return TableBuildResult.Unsupported(
                "Execution IR hash-mark lowering requires the right side to be the hash build side.");
        }

        if (CanUsePayloadFreeMarkKeySet(context))
        {
            var keySetResult = TryBuildMarkKeySetJoinTable(context);
            if (keySetResult != null)
                return keySetResult;
        }

        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var nullAlias = RowShapeLookup.ResolveSourceAlias(context.Sources.Right.Shape);
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

        var hasMatch = new ExecutionVariable($"{context.Hash.Name}HasMatch", typeof(bool));
        var matchedBody = CreateMarkJoinMatchedBody(
            context.Join.Residual,
            context.Pipeline.Filter,
            hasMatch,
            projection.MatchedAppendRow,
            context.ConversionLookup);
        return CompleteHashJoinTableBuild(
            context,
            new HashJoinTableLowering(
                resultTable,
                projection.ResultShape,
                matchedBody,
                appendBlocks.UnmatchedAppendBlock,
                hasMatch));
    }

    private ExecutionBlock CreateMarkJoinMatchedBody(
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
            ..appendBlock.Nodes,
            new ExecutionBreak()
        ]);
        return CreateConditionalJoinBlock(joinCondition, sourceLookup, matchedBlock);
    }
}
