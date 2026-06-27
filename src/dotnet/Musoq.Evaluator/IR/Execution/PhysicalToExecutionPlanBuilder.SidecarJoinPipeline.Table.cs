using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildSidecarJoinPipelineTable(
        IReadOnlyList<SidecarJoinPipelineStage> stages,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex = DefaultSchemaFromIndex)
    {
        if (stages.Count == 0)
            return null;

        var nodes = new List<ExecutionNode>();
        var shapes = new List<RowShape>();
        var activeLookup = new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase);
        var runtimeOperations = new List<SidecarJoinRuntimeOperation>(stages.Count);
        Dictionary<string, IrExpression>? currentProjectionMap = null;
        JoinSource? baseSource = null;
        ProjectedField[]? finalFields = null;
        IReadOnlyDictionary<string, RowShape>? finalOutputLookup = null;

        for (var stageIndex = 0; stageIndex < stages.Count; stageIndex++)
        {
            var stage = stages[stageIndex];
            if (TryApplySidecarJoinProjectionStage(
                    stage,
                    currentProjectionMap,
                    activeLookup,
                    ref finalFields,
                    ref finalOutputLookup,
                    out var projectedMap))
            {
                currentProjectionMap = projectedMap;
                continue;
            }

            if (stage.Pipeline.Source is not PhysicalHashJoinNode join ||
                stage.Pipeline.Project.IsDistinct ||
                stage.Pipeline.PostOperations.Count != 0 ||
                !TryResolveSidecarBuildCteRef(join, out var buildCteRef, out var sidecar) ||
                !IsSupportedSidecarPipelineJoin(join, sidecar))
            {
                return null;
            }

            var probeSourceNode = ReferenceEquals(join.Left, buildCteRef)
                ? join.Right
                : join.Left;
            PhysicalCteRefNode? probeCteRef = null;

            if (stage.ExpectedInputCteName == null)
            {
                if (baseSource != null)
                    return null;

                var source = BuildJoinSource(
                    probeSourceNode,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndex,
                    CreateSourceRowsScope(resultTableName));
                if (!source.Supported)
                    return TableBuildResult.Unsupported(source.UnsupportedReason);

                baseSource = source.Source;
                nodes.AddRange(baseSource.Setup);
                AddJoinSourceShapes(shapes, baseSource);
                AddSourceShape(activeLookup, baseSource.Shape);
            }
            else
            {
                if (probeSourceNode is not PhysicalCteRefNode cteRef ||
                    !string.Equals(cteRef.CteName, stage.ExpectedInputCteName, StringComparison.OrdinalIgnoreCase) ||
                    currentProjectionMap == null)
                {
                    return null;
                }

                probeCteRef = cteRef;
            }

            var buildSource = BuildJoinSource(
                buildCteRef,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                CreateSourceRowsScope(resultTableName));
            if (!buildSource.Supported)
                return TableBuildResult.Unsupported(buildSource.UnsupportedReason);

            if (TryUseCteSidecarHashPayloadJoinSource(buildSource.Source, sidecar, out var payloadBuildSource))
                buildSource = SourceBuildResult.Success(payloadBuildSource);

            nodes.AddRange(buildSource.Source.Setup);
            if (sidecar.Kind == CteSidecarIndexKind.Hash)
                AddJoinSourceShapes(shapes, buildSource.Source);

            var stepLookup = CloneSourceLookup(activeLookup);
            if (!AddSourceShape(stepLookup, buildSource.Source.Shape))
                return null;

            var rewrittenProbeKeys = RewriteSidecarJoinExpressions(join.ProbeKeys, currentProjectionMap, probeCteRef);
            if (rewrittenProbeKeys == null)
                return null;

            var rewrittenResidual = RewriteSidecarJoinExpression(join.Residual, currentProjectionMap, probeCteRef);
            if (join.Residual != null && rewrittenResidual == null)
                return null;

            var rewrittenFilter = RewriteSidecarJoinFilter(stage.Pipeline.Filter, currentProjectionMap, probeCteRef);
            if (stage.Pipeline.Filter != null && rewrittenFilter == null)
                return null;

            var outputLookup = join.Kind == JoinKind.Inner
                ? stepLookup
                : CloneSourceLookup(activeLookup);
            var rewrittenFields = RewriteSidecarJoinProjectedFields(
                stage.Pipeline.Project.Fields,
                currentProjectionMap,
                probeCteRef);
            if (rewrittenFields == null)
                return null;

            var indexVariable = CreateSidecarJoinIndexVariable(
                resultTableName,
                stage.OutputCteName,
                buildSource.Source.Variable.Name,
                sidecar.Kind,
                stageIndex);
            var matchesVariable = sidecar.Kind == CteSidecarIndexKind.Hash
                ? new ExecutionVariable($"{indexVariable.Name}Matches", typeof(object))
                : null;
            var introducedAliases = CreateSidecarJoinIntroducedAliases(sidecar, buildSource.Source.Shape);
            var stepFilter = rewrittenFilter;
            SidecarJoinRuntimeGuard? filterGuard = null;
            if (rewrittenFilter != null &&
                TryCreateSidecarJoinRuntimeGuard(
                    rewrittenFilter.Predicate,
                    outputLookup,
                    runtimeOperations.Count + 1,
                    out filterGuard))
            {
                stepFilter = null;
            }

            nodes.Add(CreateSidecarJoinIndexLoad(indexVariable, buildSource.Source, sidecar));
            runtimeOperations.Add(new SidecarJoinRuntimeStep(
                join,
                sidecar,
                buildSource.Source,
                indexVariable,
                matchesVariable,
                rewrittenProbeKeys,
                rewrittenResidual,
                stepFilter,
                stepLookup,
                CreateSidecarJoinRequiredAliases(
                    stepLookup,
                    introducedAliases,
                    [..rewrittenProbeKeys, rewrittenResidual, stepFilter?.Predicate]),
                introducedAliases,
                runtimeOperations.Count));
            if (filterGuard != null)
                runtimeOperations.Add(filterGuard);

            if (stage.OutputCteName == null)
            {
                finalFields = rewrittenFields;
                finalOutputLookup = outputLookup;
            }
            else
            {
                currentProjectionMap = CreateProducerProjectionExpressionMap(rewrittenFields);
                activeLookup = CloneSourceLookup(outputLookup);
            }
        }

        if (baseSource == null || finalFields == null || finalOutputLookup == null)
            return null;

        var resultShape = CreateGeneratedShape(resultShapeName, finalFields, finalOutputLookup);
        var resultTable = new ExecutionVariable(resultTableName, typeof(object));
        var appendRow = CreateSidecarJoinAppendRow(resultTable, resultShape, finalFields, finalOutputLookup);
        var body = CreateSidecarJoinRuntimeBody(
            runtimeOperations,
            baseSource.Shape,
            new ExecutionBlock([appendRow]));

        nodes.InsertRange(
            baseSource.Setup.Count,
            [CreateTable(resultTable, resultShape, CreateJoinResultCapacityCandidate(resultTable, baseSource))]);
        nodes.Add(CreateSourceLoop(baseSource.Shape, baseSource.Rows, baseSource.Variable, body));

        shapes.Add(resultShape);
        return CompleteTableBuild(
            shapes,
            nodes,
            resultTable,
            resultShape,
            [],
            isDistinct: false);
    }

    private static ExecutionAppendRow CreateSidecarJoinAppendRow(
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var values = fields
            .Select(field => new ExecutionRowValue(field.OutputName, ConvertProjectedExpression(field, sourceLookup)))
            .ToArray();
        var contextSegments = CreateSidecarJoinContextSegments(sourceLookup);

        if (contextSegments == null)
        {
            return new ExecutionAppendRow(
                resultTable,
                resultShape,
                values,
                CreateContextValues(sourceLookup),
                SerialAppendMode,
                CreateContextLayout(sourceLookup));
        }

        if (contextSegments.Count == 0)
        {
            return new ExecutionAppendRow(
                resultTable,
                resultShape,
                values,
                [],
                SerialAppendMode,
                null);
        }

        var contextArray = new ExecutionContextArray(contextSegments);
        return new ExecutionAppendRow(
            resultTable,
            resultShape,
            values,
            [contextArray],
            SerialAppendMode,
            new ExecutionContextLayout(
            [
                new ExecutionContextSegment(
                    ExecutionContextSegmentKind.Array,
                    contextArray,
                    1)
            ]));
    }

    private static IReadOnlyList<ExecutionContextSegment>? CreateSidecarJoinContextSegments(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var segments = new List<ExecutionContextSegment>(sourceLookup.Count);

        foreach (var sourceShape in sourceLookup.Values)
        {
            if (!TryCreateContextSegment(sourceShape, null, out var segment))
                return null;

            if (segment != null)
                segments.Add(segment);
        }

        return segments;
    }
}
