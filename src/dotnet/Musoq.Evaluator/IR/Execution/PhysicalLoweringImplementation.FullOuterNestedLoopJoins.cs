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
        var rightHasMatch = new ExecutionVariable($"{resultTableName}RightHasMatch", typeof(bool));
        var matchedBody = CreateOuterHashJoinMatchedBody(
            join.OnPredicate,
            pipeline.Filter,
            leftHasMatch,
            projection.MatchedAppendRow,
            sourceLookup);
        var rightMatchFlagBody = CreateFullOuterNestedLoopMatchFlagBody(
            join.OnPredicate,
            rightHasMatch,
            sourceLookup);
        var nodes = CreateJoinPrelude(joinSources, resultTable, projection.ResultShape);
        var leftRows = CreateFullOuterReusableRows(joinSources.Left, nodes);
        var rightRows = CreateFullOuterReusableRows(joinSources.Right, nodes);
        var leftPhaseInnerLoop = CreateSourceLoop(
            joinSources.Right.Shape,
            rightRows,
            joinSources.Right.Variable,
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
        var rightPhaseInnerLoop = CreateSourceLoop(
            joinSources.Left.Shape,
            leftRows,
            joinSources.Left.Variable,
            rightMatchFlagBody);
        var rightPhase = CreateSourceLoop(
            joinSources.Right.Shape,
            rightRows,
            joinSources.Right.Variable,
            new ExecutionBlock(
            [
                new ExecutionLet(rightHasMatch, new ExecutionLiteral(false, typeof(bool))),
                rightPhaseInnerLoop,
                new ExecutionIf(
                    new ExecutionUnary(
                        UnaryOpKind.Not,
                        new ExecutionVariableRead(rightHasMatch),
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

    private static ExecutionBlock CreateFullOuterNestedLoopMatchFlagBody(
        IrExpression? joinCondition,
        ExecutionVariable hasMatch,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return CreateConditionalJoinBlock(
            joinCondition,
            sourceLookup,
            new ExecutionBlock([new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool)))]));
    }

    private static ExecutionExpression CreateFullOuterReusableRows(
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
