using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;
namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildAsOfJoinTable(
        PhysicalNestedLoopJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
    {
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
        if (!CanUseAsOfProbeSource(joinSources.Right.Shape, joinSources.Right.Variable.Type))
        {
            return TableBuildResult.Unsupported(
                $"Execution IR ASOF join lowering requires a non-dynamic source-entity or table-row right source. Found {joinSources.Right.Shape.GetType().Name} with row type {FormatTypeName(joinSources.Right.Variable.Type)}.");
        }

        var leftAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Left.Shape);
        var rightAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
        var predicate = ExtractAsOfJoinPredicate(join.OnPredicate, leftAlias, rightAlias);
        if (!predicate.Supported)
            return TableBuildResult.Unsupported(predicate.UnsupportedReason);

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var asOfProbe = join.Kind == JoinKind.AsofInner
            ? CreateInnerAsOfProbe(joinSources, predicate.Value, join.TieBreak, pipeline, resultTable, resultShapeName, sourceLookup)
            : CreateLeftAsOfProbe(joinSources, predicate.Value, join.TieBreak, pipeline, resultTable, resultShapeName, sourceLookup);
        if (!asOfProbe.Supported)
            return TableBuildResult.Unsupported(asOfProbe.UnsupportedReason);
        var nodes = CreateJoinPrelude(joinSources, resultTable, asOfProbe.ResultShape);
        var asOfIndex = CreateAsOfIndex(resultTableName, asOfProbe.Probe);
        var indexedProbe = asOfProbe.Probe with { Index = asOfIndex.Index };
        var leftLoop = CreateSourceLoop(
            joinSources.Left.Shape,
            joinSources.Left.Rows,
            joinSources.Left.Variable,
            new ExecutionBlock([indexedProbe]));

        nodes.Add(asOfIndex);
        nodes.Add(leftLoop);

        return CompleteTableBuild(
            [..joinSources.Left.Shapes, ..joinSources.Right.Shapes, asOfProbe.ResultShape],
            nodes,
            resultTable,
            asOfProbe.ResultShape,
            pipeline.PostOperations,
            pipeline.Project.IsDistinct);
    }

    private static ExecutionCreateAsOfIndex CreateAsOfIndex(
        string resultTableName,
        ExecutionAsOfProbe probe)
    {
        var index = new ExecutionVariable($"{resultTableName}AsOfIndex", typeof(object));

        return new ExecutionCreateAsOfIndex(
            index,
            probe.Candidate,
            probe.Candidates,
            probe.EqualityKeys,
            probe.CandidateKey,
            probe.ComparisonKind,
            probe.ComparisonKeyType ?? typeof(object),
            probe.TieBreak);
    }

    private AsOfProbeBuildResult CreateInnerAsOfProbe(
        JoinSources joinSources,
        AsOfJoinPredicateParts predicate,
        OrderField? tieBreak,
        SupportedPipeline pipeline,
        ExecutionVariable resultTable,
        string resultShapeName,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var resultShape = CreateGeneratedShape(resultShapeName, pipeline.Project.Fields, sourceLookup);
        var appendRow = CreateAppendRow(resultTable, resultShape, pipeline.Project.Fields, sourceLookup);
        var body = CreateLoopBody(pipeline.Filter, appendRow, sourceLookup);

        try
        {
            var probe = CreateAsOfProbe(joinSources.Right, predicate, tieBreak, sourceLookup, body, null);

            return AsOfProbeBuildResult.Success(resultShape, probe);
        }
        catch (NotSupportedException ex)
        {
            return AsOfProbeBuildResult.Unsupported(ex.Message);
        }
    }

    private AsOfProbeBuildResult CreateLeftAsOfProbe(
        JoinSources joinSources,
        AsOfJoinPredicateParts predicate,
        OrderField? tieBreak,
        SupportedPipeline pipeline,
        ExecutionVariable resultTable,
        string resultShapeName,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var rightAlias = RowShapeLookup.ResolveSourceAlias(joinSources.Right.Shape);
        var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
            resultShapeName,
            resultTable,
            pipeline.Project.Fields,
            sourceLookup,
            rightAlias));
        if (!projection.Supported)
            return AsOfProbeBuildResult.Unsupported(projection.UnsupportedReason);

        var appendBlocks = CreateOuterApplyAppendBlocks(
            pipeline.Filter,
            projection.MatchedAppendRow,
            projection.UnmatchedAppendRow,
            sourceLookup,
            rightAlias);
        if (!appendBlocks.Supported)
            return AsOfProbeBuildResult.Unsupported(appendBlocks.UnsupportedReason);

        try
        {
            var probe = CreateAsOfProbe(
                joinSources.Right,
                predicate,
                tieBreak,
                sourceLookup,
                appendBlocks.MatchedAppendBlock,
                appendBlocks.UnmatchedAppendBlock);

            return AsOfProbeBuildResult.Success(projection.ResultShape, probe);
        }
        catch (NotSupportedException ex)
        {
            return AsOfProbeBuildResult.Unsupported(ex.Message);
        }
    }

    private static ExecutionAsOfProbe CreateAsOfProbe(
        JoinSource rightSource,
        AsOfJoinPredicateParts predicate,
        OrderField? tieBreak,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        ExecutionBlock body,
        ExecutionBlock? noMatchBody)
    {
        var rightAlias = RowShapeLookup.ResolveSourceAlias(rightSource.Shape);
        var candidate = new ExecutionVariable(
            $"{rightSource.Variable.Name}Candidate",
            rightSource.Variable.Type,
            rightSource.Variable.GeneratedRowTypeName);
        var equalityKeys = predicate.EqualityKeys
            .Select(key => new ExecutionAsOfEqualityKey(
                ExecutionExpressionConverter.Convert(key.Left, sourceLookup),
                ReplaceExecutionAlias(
                    ExecutionExpressionConverter.Convert(key.Right, sourceLookup),
                    rightAlias,
                    candidate.Name)))
            .ToArray();

        var probeKey = ExecutionExpressionConverter.Convert(predicate.LeftInequalityKey, sourceLookup);
        var candidateKey = ReplaceExecutionAlias(
            ExecutionExpressionConverter.Convert(predicate.RightInequalityKey, sourceLookup),
            rightAlias,
            candidate.Name);
        var executionTieBreak = CreateAsOfTieBreak(tieBreak, sourceLookup, rightAlias, candidate.Name);

        return new ExecutionAsOfProbe(
            rightSource.Variable,
            candidate,
            rightSource.Rows,
            equalityKeys,
            probeKey,
            candidateKey,
            predicate.ComparisonKind,
            body,
            noMatchBody,
            ComparisonKeyType: ResolveAsOfComparisonKeyType(probeKey.ReturnType, candidateKey.ReturnType),
            TieBreak: executionTieBreak);
    }

    private static ExecutionAsOfTieBreak? CreateAsOfTieBreak(
        OrderField? tieBreak,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string rightAlias,
        string candidateName)
    {
        if (tieBreak == null)
            return null;

        var key = ReplaceExecutionAlias(
            ExecutionExpressionConverter.Convert(tieBreak.Expression, sourceLookup),
            rightAlias,
            candidateName);

        return new ExecutionAsOfTieBreak(key, tieBreak.Descending, tieBreak.NullOrdering);
    }

    private static Type ResolveAsOfComparisonKeyType(Type probeKeyType, Type candidateKeyType)
    {
        if (probeKeyType == candidateKeyType && IsTypedAsOfComparisonKey(probeKeyType))
            return probeKeyType;

        return typeof(object);
    }

    private static bool IsTypedAsOfComparisonKey(Type type)
    {
        return type != typeof(object) && type is not NullNode.NullType;
    }

}
