using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildWindowTable(
        WindowPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope
    )
    {
        var registrationsResult = ResolveSupportedWindowRegistrations(pipeline.Registrations);
        if (!registrationsResult.IsBuilt)
            return TableBuildResult.Unsupported(registrationsResult.UnsupportedReason);

        var windowSource = BuildWindowSource(
            pipeline.Source.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            scope
        );
        if (!windowSource.IsBuilt)
            return TableBuildResult.Unsupported(windowSource.UnsupportedReason);

        var sourceShape = windowSource.Source.Shape;
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var aggregateSourceFields = CreateWindowAggregateSourceFieldLookup(pipeline.Source.Source);
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            sourceLookup
        );
        if (!projection.IsBuilt)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection =
            projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultShape = postOperationProjection.WorkingShape;
        var source = windowSource.Source.Variable;
        var sourceSetup = windowSource.Source.Setup;
        var sourceRows = windowSource.Source.Rows;
        var resultTable = postOperationProjection.WorkingTable;
        var buffer = CreateWindowRowsBufferVariable(
            $"{resultTableName}WindowRows",
            source,
            windowSource.Source.GeneratedRowShape
        );
        var windowIndex = new ExecutionVariable("windowIndex", typeof(int));
        var rowAccessMode = ExecutionRowAccessMode.Direct;
        var materialization = CreateWindowMaterialization(
            new WindowMaterializationContext(
                pipeline.Source,
                sourceRows,
                buffer,
                source,
                sourceShape,
                rowAccessMode,
                sourceLookup,
                windowSource.Source.GeneratedRowShape
            )
        );

        if (!materialization.IsBuilt)
            return TableBuildResult.Unsupported(materialization.UnsupportedReason);

        var qualifyTopRankPlan = WindowQualifyBounds.Create(pipeline.QualifyPredicate, registrationsResult.Value);

        var qualifiedPosition = pipeline.QualifyPredicate is null
            ? null
            : registrationsResult.Value.FirstOrDefault(item => item.Registration!.NumbersQualifiedRows);
        var computations = CreateWindowComputations(
            qualifiedPosition is null
                ? registrationsResult.Value
                : registrationsResult.Value.Where(item => item != qualifiedPosition).ToArray(),
            buffer,
            source,
            rowAccessMode,
            sourceLookup,
            aggregateSourceFields,
            resultTableName,
            qualifyTopRankPlan.UpperBounds
        );

        if (!computations.IsBuilt)
            return TableBuildResult.Unsupported(computations.UnsupportedReason);

        var windowResults = computations.Value.ToDictionary(
            computation => computation.Registration!.WindowIndex,
            computation => computation.Results
        );

        IReadOnlyList<ExecutionNode> positionSetup = [];
        ExecutionVariable? qualifiedMask = null;
        if (qualifyTopRankPlan.Predicate is { } predicate && qualifiedPosition is not null)
        {
            var condition = ConvertWindowExpression(
                predicate,
                sourceLookup,
                aggregateSourceFields,
                windowResults,
                windowIndex
            );
            if (!condition.IsBuilt)
                return TableBuildResult.Unsupported(condition.UnsupportedReason);
            qualifiedMask = new ExecutionVariable(resultTableName + "QualifiedRows", typeof(bool[]));
            var qualifiedPositions = new ExecutionVariable(resultTableName + "QualifiedPositions", typeof(long[]));
            var survivors = CreateWindowRowsBufferVariable(
                resultTableName + "Survivors",
                source,
                windowSource.Source.GeneratedRowShape
            );
            var position = CreateWindowComputations(
                [qualifiedPosition],
                survivors,
                source,
                rowAccessMode,
                sourceLookup,
                aggregateSourceFields,
                resultTableName + "Survivors",
                new Dictionary<int, long>()
            );
            if (!position.IsBuilt)
                return TableBuildResult.Unsupported(position.UnsupportedReason);
            positionSetup = CreateQualifiedPositions(
                source,
                windowIndex,
                buffer,
                qualifiedMask,
                qualifiedPositions,
                survivors,
                windowSource.Source.GeneratedRowShape,
                position.Value,
                condition.Value
            );
            windowResults[qualifiedPosition.Registration!.WindowIndex] = qualifiedPositions;
        }

        var preserveWindowAppendContexts =
            ShouldPreserveWindowAppendContexts(pipeline)
            || !string.Equals(resultTableName, "result", StringComparison.Ordinal);
        var appendRow = CreateWindowAppendRow(
            resultTable,
            resultShape,
            postOperationProjection.MaterializedFields,
            sourceLookup,
            aggregateSourceFields,
            windowResults,
            windowIndex,
            preserveWindowAppendContexts
        );

        if (!appendRow.IsBuilt)
            return TableBuildResult.Unsupported(appendRow.UnsupportedReason);

        var appendBlock = qualifiedMask is not null
            ? LoweringAttempt<ExecutionBlock>.Built(
                CreateFilteredAppendBlock(
                    new ExecutionWindowValueRead(qualifiedMask, windowIndex, typeof(bool)),
                    appendRow.Value
                )
            )
            : CreateWindowAppendBlock(
                qualifyTopRankPlan.Predicate,
                appendRow.Value,
                sourceLookup,
                aggregateSourceFields,
                windowResults,
                windowIndex
            );

        if (!appendBlock.IsBuilt)
            return TableBuildResult.Unsupported(appendBlock.UnsupportedReason);

        var boundedRanking =
            qualifiedPosition is null
            && computations.Value is [{ Node: ExecutionComputeRankingWindow { WinnerPositions: not null } ranking }]
                ? ranking
                : null;
        var windowKernelNodes = CreateWindowKernelPlanNodes(computations.Value);
        var nodes = new List<ExecutionNode>(
            sourceSetup.Count + pipeline.PostOperations.Count + windowKernelNodes.Count + 4
        );
        nodes.AddRange(sourceSetup);
        nodes.Add(materialization.Value);
        nodes.AddRange(windowKernelNodes);
        nodes.AddRange(positionSetup);
        nodes.Add(
            CreateTable(
                resultTable,
                resultShape,
                ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(
                    resultTable,
                    boundedRanking?.Results ?? buffer
                )
            )
        );
        nodes.Add(
            new ExecutionForEachIndexed(source, windowIndex, buffer, rowAccessMode, appendBlock.Value)
            {
                WinnerPositions = boundedRanking?.WinnerPositions,
                WinnerRanks = boundedRanking?.Results,
            }
        );

        return CompleteTableBuild(
            [.. windowSource.Source.Shapes, .. postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection
        );
    }

    private static ExecutionVariable CreateWindowRowsBufferVariable(
        string name,
        ExecutionVariable source,
        GeneratedRowShape? generatedRowShape
    )
    {
        return string.IsNullOrWhiteSpace(generatedRowShape?.TypeName)
            ? new ExecutionVariable(name, CreateWindowBufferType(source))
            : CreateMaterializedRowsBufferVariable(name, generatedRowShape);
    }

    private static Type CreateWindowBufferType(ExecutionVariable source)
    {
        return string.IsNullOrWhiteSpace(source.GeneratedRowTypeName)
            ? typeof(List<>).MakeGenericType(source.Type.ResolveClrType())
            : typeof(object);
    }

    private static bool ShouldPreserveWindowAppendContexts(WindowPipeline pipeline)
    {
        return pipeline.QualifyPredicate != null || pipeline.PostOperations.Count != 0 || pipeline.Project.IsDistinct;
    }
}
