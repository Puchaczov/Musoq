using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
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
        if (join.Kind != JoinKind.Inner)
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
        var resultShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, sourceLookup);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, pipeline.Project.Fields, sourceLookup);
        var matchedBody = CreateJoinLoopBody(join.Residual, pipeline.Filter, appendRow, sourceLookup);
        var rightAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
        var candidate = new ExecutionVariable($"{joinSources.Right.Variable.Name}RangeCandidate", joinSources.Right.Variable.Type);
        var rangeIndex = new ExecutionVariable($"{resultTableName}RangeIndex", typeof(object));
        var keyType = ResolveRangeJoinKeyType(join);
        var candidateKey = ReplaceExecutionAlias(
            ExecutionExpressionConverter.Convert(join.RightKey, sourceLookup),
            rightAlias,
            candidate.Name);
        var createIndex = new ExecutionCreateRangeIndex(
            rangeIndex,
            candidate,
            joinSources.Right.Rows,
            candidateKey,
            keyType,
            join.ComparisonKind);
        var rangeProbe = new ExecutionRangeProbe(
            joinSources.Right.Variable,
            rangeIndex,
            ExecutionExpressionConverter.Convert(join.LeftKey, sourceLookup),
            keyType,
            matchedBody);
        var leftLoop = CreateSourceLoop(
            joinSources.Left.Shape,
            joinSources.Left.Rows,
            joinSources.Left.Variable,
            new ExecutionBlock([rangeProbe]));
        var nodes = CreateJoinPrelude(joinSources, resultTable, resultShape);

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
}
