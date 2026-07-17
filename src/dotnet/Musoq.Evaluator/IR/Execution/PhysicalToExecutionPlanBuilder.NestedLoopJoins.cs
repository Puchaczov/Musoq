using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
    {
        if (join.Kind is JoinKind.AsofInner or JoinKind.AsofLeft)
        {
            return BuildAsOfJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                session);
        }

        var sources = BuildJoinSources(
            join.Left,
            join.Right,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            session);
        if (!sources.Supported)
            return TableBuildResult.Unsupported(sources.UnsupportedReason);

        return join.Kind switch
        {
            JoinKind.Inner or JoinKind.Cross => BuildInnerNestedLoopJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                sources.Source),
            JoinKind.LeftOuter or JoinKind.RightOuter or JoinKind.LeftMark => BuildOuterNestedLoopJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                sources.Source),
            JoinKind.FullOuter => BuildFullOuterNestedLoopJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                sources.Source),
            JoinKind.LeftSemi => BuildSemiNestedLoopJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                sources.Source,
                isAntiSemiJoin: false),
            JoinKind.LeftAntiSemi => BuildSemiNestedLoopJoinTable(
                join,
                pipeline,
                resultTableName,
                resultShapeName,
                sources.Source,
                isAntiSemiJoin: true),
            _ => UnsupportedJoinKind(join.Kind)
        };
    }

    private TableBuildResult BuildInnerNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        JoinSources joinSources)
    {
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var resultShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, sourceLookup);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, pipeline.Project.Fields, sourceLookup);
        var joinCondition = join.Kind == JoinKind.Cross ? null : join.OnPredicate;
        var joinBody = CreateJoinLoopBody(joinCondition, pipeline.Filter, appendRow, sourceLookup);
        var nodes = CreateJoinPrelude(joinSources, resultTable, resultShape);
        var rightRows = CreateNestedLoopInnerRows(joinSources.Right, nodes);
        var rightLoop = CreateSourceLoop(joinSources.Right.Shape, rightRows, joinSources.Right.Variable, joinBody);
        var leftLoop = CreateSourceLoop(
            joinSources.Left.Shape,
            joinSources.Left.Rows,
            joinSources.Left.Variable,
            new ExecutionBlock([rightLoop]));

        nodes.Add(leftLoop);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, resultShape],
            nodes,
            resultTable,
            resultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

    private static string ResolveOuterJoinNullAlias(
        JoinKind kind,
        JoinSources joinSources)
    {
        return kind switch
        {
            JoinKind.LeftOuter => RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape),
            JoinKind.RightOuter => RowShapeLookup.ResolveSourceAlias(joinSources.Left.Shape),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, @"Only outer join kinds have a null-extended side.")
        };
    }

    private static OuterNestedLoopSides ResolveOuterNestedLoopSides(
        JoinKind kind,
        JoinSources joinSources)
    {
        return kind switch
        {
            JoinKind.LeftOuter or JoinKind.LeftMark => new OuterNestedLoopSides(joinSources.Left, joinSources.Right),
            JoinKind.RightOuter => new OuterNestedLoopSides(joinSources.Right, joinSources.Left),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, @"Only outer join kinds have preserved and nullable sides.")
        };
    }

    private TableBuildResult BuildOuterNestedLoopJoinTable(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        JoinSources joinSources)
    {
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var nullAlias = ResolveOuterJoinNullAlias(join.Kind, joinSources);
        var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
            resultShapeName,
            resultTable,
            pipeline.Project.Fields,
            sourceLookup,
            nullAlias));
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var appendBlocks = CreateOuterApplyAppendBlocks(
            pipeline.Filter,
            projection.MatchedAppendRow,
            projection.UnmatchedAppendRow,
            sourceLookup,
            nullAlias);
        if (!appendBlocks.Supported)
            return TableBuildResult.Unsupported(appendBlocks.UnsupportedReason);

        var hasMatch = new ExecutionVariable($"{resultTableName}HasMatch", typeof(bool));
        var matchedBody = join.Kind == JoinKind.LeftMark
            ? CreateMarkJoinMatchedBody(
                join.OnPredicate,
                pipeline.Filter,
                hasMatch,
                projection.MatchedAppendRow,
                sourceLookup)
            : CreateOuterHashJoinMatchedBody(
                join.OnPredicate,
                pipeline.Filter,
                hasMatch,
                projection.MatchedAppendRow,
                sourceLookup);
        var sides = ResolveOuterNestedLoopSides(join.Kind, joinSources);
        var nodes = CreateJoinPrelude(joinSources, resultTable, projection.ResultShape);
        var innerRows = CreateNestedLoopInnerRows(sides.Inner, nodes);
        var innerLoop = CreateSourceLoop(
            sides.Inner.Shape,
            innerRows,
            sides.Inner.Variable,
            matchedBody);
        var unmatchedBody = new ExecutionIf(
            new ExecutionUnary(
                UnaryOpKind.Not,
                new ExecutionVariableRead(hasMatch),
                typeof(bool)),
            appendBlocks.UnmatchedAppendBlock);
        var outerLoop = CreateSourceLoop(
            sides.Outer.Shape,
            sides.Outer.Rows,
            sides.Outer.Variable,
            new ExecutionBlock(
            [
                new ExecutionLet(hasMatch, new ExecutionLiteral(false, typeof(bool))),
                innerLoop,
                unmatchedBody
            ]));
        nodes.Add(outerLoop);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, projection.ResultShape],
            nodes,
            resultTable,
            projection.ResultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

}
