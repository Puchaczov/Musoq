using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildFullOuterNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        JoinSources joinSources)
    {
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var leftAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Left.Shape);
        var rightAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
        var projection = CreateFullOuterNullExtendedProjection(new NullExtendedProjectionContext(
                resultShapeName,
                resultTable,
                pipeline.Project.Fields,
                sourceLookup,
                rightAlias),
            leftAlias,
            rightAlias);
        if (!projection.IsBuilt)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var leftOnlyAppendBlocks = CreateOuterApplyAppendBlocks(
            pipeline.Filter,
            projection.MatchedAppendRow,
            projection.LeftOnlyAppendRow,
            sourceLookup,
            rightAlias);
        if (!leftOnlyAppendBlocks.IsBuilt)
            return TableBuildResult.Unsupported(leftOnlyAppendBlocks.UnsupportedReason);

        var rightOnlyAppendBlocks = CreateOuterApplyAppendBlocks(
            pipeline.Filter,
            projection.MatchedAppendRow,
            projection.RightOnlyAppendRow,
            sourceLookup,
            leftAlias);
        if (!rightOnlyAppendBlocks.IsBuilt)
            return TableBuildResult.Unsupported(rightOnlyAppendBlocks.UnsupportedReason);

        var leftHasMatch = new ExecutionVariable($"{resultTableName}LeftHasMatch", typeof(bool));
        var rightMatched = new ExecutionVariable($"{resultTableName}RightMatched", typeof(bool[]));
        var rightIndex = new ExecutionVariable($"{joinSources.Right.Variable.Name}Index", typeof(int));
        var matchedBody = CreateFullOuterJoinMatchedBody(
            join.OnPredicate,
            pipeline.Filter,
            leftHasMatch,
            rightMatched,
            rightIndex,
            projection.MatchedAppendRow,
            sourceLookup);
        var nodes = CreateJoinPrelude(joinSources, resultTable, projection.ResultShape);
        var leftRows = CreateFullOuterReusableRows(joinSources.Left, nodes);
        var rightRows = CreateFullOuterReusableRows(joinSources.Right, nodes);
        nodes.Add(new ExecutionCreateBooleanArray(rightMatched, rightRows.Variable));
        var leftPhaseInnerLoop = ExecutionRowStreams.CreateForEachIndexed(
            joinSources.Right.Shape,
            rightRows.Variable,
            joinSources.Right.Variable,
            rightIndex,
            matchedBody);
        var leftPhase = CreateSourceLoop(
            joinSources.Left.Shape,
            leftRows,
            joinSources.Left.Variable,
            new ExecutionBlock(
            [
                new ExecutionLet(leftHasMatch, new ExecutionLiteral(false, typeof(bool))),
                leftPhaseInnerLoop,
                new ExecutionIf(
                    new ExecutionUnary(
                        UnaryOpKind.Not,
                        new ExecutionVariableRead(leftHasMatch),
                        typeof(bool)),
                    leftOnlyAppendBlocks.UnmatchedAppendBlock)
            ]));
        var rightPhase = ExecutionRowStreams.CreateForEachIndexed(
            joinSources.Right.Shape,
            rightRows.Variable,
            joinSources.Right.Variable,
            rightIndex,
            new ExecutionBlock(
            [
                new ExecutionIf(
                    new ExecutionUnary(
                        UnaryOpKind.Not,
                        new ExecutionArrayAccess(
                            new ExecutionVariableRead(rightMatched),
                            new ExecutionVariableRead(rightIndex),
                            typeof(bool),
                            typeof(bool)),
                        typeof(bool)),
                    rightOnlyAppendBlocks.UnmatchedAppendBlock)
            ]));

        nodes.Add(leftPhase);
        nodes.Add(rightPhase);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, projection.ResultShape],
            nodes,
            resultTable,
            projection.ResultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

    private static ExecutionVariableRead CreateFullOuterReusableRows(
        JoinSource source,
        List<ExecutionNode> nodes)
    {
        var buffer = CreateMaterializedRowsBufferVariable(
            CreateIdentifierCandidate($"{source.Variable.Name}RowsBuffer", 0),
            source.GeneratedRowShape);

        nodes.Add(CreateMaterializeListNode(source.Rows, buffer, source.GeneratedRowShape));

        return new ExecutionVariableRead(buffer);
    }
}
