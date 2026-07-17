using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static readonly MethodInfo ThrowScalarSubqueryCardinalityViolationMethod = typeof(EvaluationHelper)
        .GetMethod(nameof(EvaluationHelper.ThrowScalarSubqueryCardinalityViolation))!;

    private TableBuildResult BuildSortMergeJoinTable(
        PhysicalSortMergeJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
    {
        if (join.Kind is not (JoinKind.Inner or JoinKind.LeftSemi or JoinKind.LeftAntiSemi or JoinKind.LeftMark or JoinKind.LeftSingle))
            return UnsupportedJoinKind(join.Kind);

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

        var joinSources = sources.Source;
        if (!CanUseAsOfProbeSource(joinSources.Right.Shape, joinSources.Right.Variable.Type.ClrType))
        {
            return TableBuildResult.Unsupported(
                "Execution IR sort-merge join lowering received a right input that cannot be range-probed. Physical planning must select nested-loop before Execution IR lowering.");
        }

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        GeneratedRowShape resultShape;
        ExecutionBlock matchedBody;
        ExecutionBlock? noMatchBody = null;
        ExecutionVariable? matchFound = null;
        IReadOnlyList<ExecutionNode>? preludeNodes = null;
        if (join.Kind is JoinKind.Inner or JoinKind.LeftSemi or JoinKind.LeftAntiSemi)
        {
            resultShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, sourceLookup);
            var appendRow = CreateAppendRow(resultTable, resultShape, pipeline.Project.Fields, sourceLookup);
            if (join.Kind == JoinKind.Inner)
            {
                matchedBody = CreateJoinLoopBody(join.Residual, pipeline.Filter, appendRow, sourceLookup);
            }
            else if (join.Kind == JoinKind.LeftSemi)
            {
                matchedBody = CreateSemiRangeJoinMatchedBody(
                    join.Residual,
                    pipeline.Filter,
                    appendRow,
                    sourceLookup);
            }
            else
            {
                matchFound = new ExecutionVariable($"{resultTableName}RangeHasMatch", typeof(bool));
                matchedBody = CreateAntiRangeJoinMatchedBody(join.Residual, matchFound, sourceLookup);
                noMatchBody = CreateOuterJoinMatchedAppendBlock(
                    pipeline.Filter,
                    appendRow,
                    sourceLookup);
            }
        }
        else
        {
            var nullAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
            IReadOnlyDictionary<string, ExecutionExpression>? emptyDefaults = null;
            if (join.Kind == JoinKind.LeftSingle &&
                TryCreateScalarSubqueryEmptyResultLowering(
                    session,
                    join.Right,
                    $"{resultTableName}Range",
                    out var valueColumnName,
                    out var emptyLowering))
            {
                emptyDefaults = new Dictionary<string, ExecutionExpression>(StringComparer.OrdinalIgnoreCase)
                {
                    [valueColumnName] = emptyLowering.Value
                };
                preludeNodes = emptyLowering.PreludeNodes;
            }

            var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
                resultShapeName,
                resultTable,
                pipeline.Project.Fields,
                sourceLookup,
                nullAlias,
                emptyDefaults));
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

            resultShape = projection.ResultShape;
            matchFound = new ExecutionVariable($"{resultTableName}RangeHasMatch", typeof(bool));
            matchedBody = join.Kind == JoinKind.LeftMark
                ? CreateMarkJoinMatchedBody(
                    join.Residual,
                    pipeline.Filter,
                    matchFound,
                    projection.MatchedAppendRow,
                    sourceLookup)
                : CreateSingleRangeJoinMatchedBody(
                    join.Residual,
                    pipeline.Filter,
                    matchFound,
                    projection.MatchedAppendRow,
                    sourceLookup);
            noMatchBody = appendBlocks.UnmatchedAppendBlock;
        }

        var rightAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
        var candidate = new ExecutionVariable(
            $"{joinSources.Right.Variable.Name}RangeCandidate",
            joinSources.Right.Variable.Type,
            joinSources.Right.Variable.GeneratedRowTypeName);
        var rangeIndex = new ExecutionVariable($"{resultTableName}RangeIndex", typeof(object));
        var keyType = ResolveRangeJoinKeyType(join);
        var partitionKeyType = ResolveRangeJoinPartitionKeyType(join);
        var candidateKey = ReplaceExecutionAlias(
            ExecutionExpressionConverter.Convert(join.RightKey, sourceLookup),
            rightAlias,
            candidate.Name);
        var partitionKeys = join.LeftPartitionKeys
            .Zip(join.RightPartitionKeys, (leftKey, rightKey) => new ExecutionAsOfEqualityKey(
                ExecutionExpressionConverter.Convert(leftKey, sourceLookup),
                ReplaceExecutionAlias(
                    ExecutionExpressionConverter.Convert(rightKey, sourceLookup),
                    rightAlias,
                    candidate.Name)))
            .ToArray();
        var createIndex = new ExecutionCreateRangeIndex(
            rangeIndex,
            candidate,
            joinSources.Right.Rows,
            candidateKey,
            keyType,
            join.ComparisonKind,
            partitionKeys,
            partitionKeyType);
        var rangeProbe = new ExecutionRangeProbe(
            joinSources.Right.Variable,
            rangeIndex,
            ExecutionExpressionConverter.Convert(join.LeftKey, sourceLookup),
            keyType,
            matchedBody,
            noMatchBody,
            matchFound,
            partitionKeys,
            partitionKeyType);
        var leftLoop = CreateSourceLoop(
            joinSources.Left.Shape,
            joinSources.Left.Rows,
            joinSources.Left.Variable,
            new ExecutionBlock([rangeProbe]));
        var nodes = CreateJoinPrelude(joinSources, resultTable, resultShape);

        if (preludeNodes != null)
            nodes.AddRange(preludeNodes);
        nodes.Add(createIndex);
        nodes.Add(leftLoop);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, resultShape],
            nodes,
            resultTable,
            resultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

    private ExecutionBlock CreateSingleRangeJoinMatchedBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionVariable hasMatch,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var cardinalityFailure = new ExecutionVariable($"{hasMatch.Name}CardinalityFailure", typeof(bool));
        var appendBlock = CreateOuterJoinMatchedAppendBlock(filter, appendRow, sourceLookup);
        var matchedBlock = new ExecutionBlock(
        [
            new ExecutionIf(
                new ExecutionVariableRead(hasMatch),
                new ExecutionBlock(
                [
                    new ExecutionLet(
                        cardinalityFailure,
                        new ExecutionMethodCall(
                            ThrowScalarSubqueryCardinalityViolationMethod,
                            [],
                            null,
                            typeof(bool)))
                ])),
            new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool))),
            ..appendBlock.Nodes
        ]);
        return CreateConditionalJoinBlock(joinCondition, sourceLookup, matchedBlock);
    }

    private ExecutionBlock CreateSemiRangeJoinMatchedBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var appendBlock = CreateOuterJoinMatchedAppendBlock(filter, appendRow, sourceLookup);
        return CreateConditionalJoinBlock(
            joinCondition,
            sourceLookup,
            new ExecutionBlock([..appendBlock.Nodes, new ExecutionBreak()]));
    }

    private ExecutionBlock CreateAntiRangeJoinMatchedBody(
        IrExpression? joinCondition,
        ExecutionVariable hasMatch,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return CreateConditionalJoinBlock(
            joinCondition,
            sourceLookup,
            new ExecutionBlock(
            [
                new ExecutionAssign(hasMatch, new ExecutionLiteral(true, typeof(bool))),
                new ExecutionBreak()
            ]));
    }
}
