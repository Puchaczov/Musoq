using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildFullOuterHashJoinTable(HashJoinBuildContext context)
    {
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var leftAlias = RowShapeLookup.ResolveSourceAlias(context.Sources.Left.Shape);
        var rightAlias = RowShapeLookup.ResolveSourceAlias(context.Sources.Right.Shape);
        var projection = CreateFullOuterNullExtendedProjection(new NullExtendedProjectionContext(
                context.ResultShapeName,
                resultTable,
                context.Pipeline.Project.Fields,
                context.SourceLookup,
                rightAlias),
            leftAlias,
            rightAlias);
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var leftOnlyAppendBlocks = CreateOuterApplyAppendBlocks(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            projection.LeftOnlyAppendRow,
            context.SourceLookup,
            rightAlias);
        if (!leftOnlyAppendBlocks.Supported)
            return TableBuildResult.Unsupported(leftOnlyAppendBlocks.UnsupportedReason);

        var rightOnlyAppendBlocks = CreateOuterApplyAppendBlocks(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            projection.RightOnlyAppendRow,
            context.SourceLookup,
            leftAlias);
        if (!rightOnlyAppendBlocks.Supported)
            return TableBuildResult.Unsupported(rightOnlyAppendBlocks.UnsupportedReason);

        var buildRows = CreateMaterializedRowsBufferVariable(
            CreateIdentifierCandidate($"{context.Sides.Build.Variable.Name}RowsBuffer", 0),
            context.Sides.Build.GeneratedRowShape);
        var buildIndex = new ExecutionVariable($"{context.Sides.Build.Variable.Name}Index", typeof(int));
        var indexedBuildRow = CreateIndexedHashRowVariable(
            $"{context.Sides.Build.Variable.Name}Indexed",
            context.Sides.Build.Variable);
        var buildMatched = new ExecutionVariable($"{context.Hash.Name}BuildMatched", typeof(bool[]));
        var probeHasMatch = new ExecutionVariable($"{context.Hash.Name}HasMatch", typeof(bool));

        var probeOnlyAppendBlock = ReferenceEquals(context.Sides.Probe, context.Sources.Left)
            ? leftOnlyAppendBlocks.UnmatchedAppendBlock
            : rightOnlyAppendBlocks.UnmatchedAppendBlock;
        var buildOnlyAppendBlock = ReferenceEquals(context.Sides.Build, context.Sources.Left)
            ? leftOnlyAppendBlocks.UnmatchedAppendBlock
            : rightOnlyAppendBlocks.UnmatchedAppendBlock;
        var matchedBody = CreateFullOuterHashJoinMatchedBody(
            context.Join.Residual,
            context.Pipeline.Filter,
            probeHasMatch,
            buildMatched,
            buildIndex,
            projection.MatchedAppendRow,
            context.ConversionLookup);
        var buildOnlyBody = new ExecutionIf(
            new ExecutionUnary(
                UnaryOpKind.Not,
                new ExecutionArrayAccess(
                    new ExecutionVariableRead(buildMatched),
                    new ExecutionVariableRead(buildIndex),
                    typeof(bool),
                    typeof(bool)),
                typeof(bool)),
            buildOnlyAppendBlock);
        var nodes = CreateFullOuterHashJoinPrelude(
            context,
            projection.ResultShape,
            resultTable,
            buildRows,
            buildMatched,
            indexedBuildRow);

        nodes.Add(CreateFullOuterHashBuildLoop(context, buildRows, buildIndex, indexedBuildRow));
        nodes.Add(CreateFullOuterHashProbeLoop(
            context,
            indexedBuildRow,
            buildIndex,
            probeHasMatch,
            matchedBody,
            probeOnlyAppendBlock));
        nodes.Add(new ExecutionForEachIndexed(
            context.Sides.Build.Variable,
            buildIndex,
            buildRows,
            ExecutionRowAccessMode.Direct,
            new ExecutionBlock([buildOnlyBody])));

        return CompleteTableBuild(
            [..context.Sources.Left.Shapes, ..context.Sources.Right.Shapes, projection.ResultShape],
            nodes,
            resultTable,
            projection.ResultShape,
            context.Pipeline.PostOperations,
            context.Pipeline.Project.IsDistinct);
    }

    private List<ExecutionNode> CreateFullOuterHashJoinPrelude(
        HashJoinBuildContext context,
        GeneratedRowShape resultShape,
        ExecutionVariable resultTable,
        ExecutionVariable buildRows,
        ExecutionVariable buildMatched,
        ExecutionVariable indexedBuildRow)
    {
        var nodes = CreateJoinPrelude(
            context.Sources,
            resultTable,
            resultShape,
            CreateJoinResultCapacityCandidate(resultTable, context.Sides.Probe));

        nodes.Add(CreateMaterializeListNode(
            context.Sides.Build.Rows,
            buildRows,
            context.Sides.Build.GeneratedRowShape));
        nodes.Add(new ExecutionCreateHash(
            context.Hash,
            context.KeyType,
            indexedBuildRow.Type.ClrType,
            new ExecutionCollectionCountCapacityHint(buildRows),
            indexedBuildRow.GeneratedRowTypeName));
        nodes.Add(new ExecutionCreateBooleanArray(buildMatched, buildRows));

        return nodes;
    }

    private static ExecutionForEachIndexed CreateFullOuterHashBuildLoop(
        HashJoinBuildContext context,
        ExecutionVariable buildRows,
        ExecutionVariable buildIndex,
        ExecutionVariable indexedBuildRow)
    {
        return new ExecutionForEachIndexed(
            context.Sides.Build.Variable,
            buildIndex,
            buildRows,
            ExecutionRowAccessMode.Direct,
            new ExecutionBlock(
            [
                new ExecutionLet(
                    indexedBuildRow,
                    new ExecutionIndexedHashRowCreate(
                        context.Sides.Build.Variable,
                        buildIndex,
                        indexedBuildRow.Type,
                        indexedBuildRow.GeneratedRowTypeName)),
                new ExecutionHashAdd(
                    context.Hash,
                    CreateHashJoinKeyExpression(context.Join.BuildKeys, context.ConversionLookup, context.KeyType),
                    indexedBuildRow,
                    context.KeyType,
                    indexedBuildRow.Type.ClrType,
                    indexedBuildRow.GeneratedRowTypeName)
            ]));
    }

    private static ExecutionSourceLoop CreateFullOuterHashProbeLoop(
        HashJoinBuildContext context,
        ExecutionVariable indexedBuildRow,
        ExecutionVariable buildIndex,
        ExecutionVariable probeHasMatch,
        ExecutionBlock matchedBody,
        ExecutionBlock probeOnlyAppendBlock)
    {
        var matchesLoop = new ExecutionForEach(
            indexedBuildRow,
            new ExecutionVariableRead(context.Matches),
            new ExecutionBlock(
            [
                new ExecutionLet(
                    context.Sides.Build.Variable,
                    new ExecutionIndexedHashRowRowRead(indexedBuildRow, context.Sides.Build.Variable.Type)),
                new ExecutionLet(
                    buildIndex,
                    new ExecutionIndexedHashRowIndexRead(indexedBuildRow)),
                ..matchedBody.Nodes
            ]));

        return CreateSourceLoop(
            context.Sides.Probe.Shape,
            context.Sides.Probe.Rows,
            context.Sides.Probe.Variable,
            new ExecutionBlock(
            [
                new ExecutionHashProbe(
                    context.Hash,
                    context.Matches,
                    CreateHashJoinKeyExpression(context.Join.ProbeKeys, context.ConversionLookup, context.KeyType),
                    context.KeyType,
                    indexedBuildRow.Type.ClrType,
                    new ExecutionBlock([matchesLoop]),
                    probeOnlyAppendBlock,
                    probeHasMatch,
                    indexedBuildRow.GeneratedRowTypeName)
            ]));
    }

    private ExecutionBlock CreateFullOuterHashJoinMatchedBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionVariable probeHasMatch,
        ExecutionVariable buildMatched,
        ExecutionVariable buildIndex,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var appendBlock = CreateOuterJoinMatchedAppendBlock(filter, appendRow, sourceLookup);
        var matchedBlock = new ExecutionBlock(
        [
            new ExecutionAssign(probeHasMatch, new ExecutionLiteral(true, typeof(bool))),
            new ExecutionArrayAssign(
                buildMatched,
                new ExecutionVariableRead(buildIndex),
                new ExecutionLiteral(true, typeof(bool)),
                typeof(bool)),
            ..appendBlock.Nodes
        ]);

        return CreateConditionalJoinBlock(joinCondition, sourceLookup, matchedBlock);
    }
}
