using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionPlanBuildResult BuildWindowPipeline(
        WindowPipeline pipeline,
        string identifier,
        PhysicalToExecutionLoweringSession session)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var table = BuildWindowTable(pipeline, "result", "ResultRow0", cteIndexes, session: session);
        if (!table.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(table.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, table));
    }

    private TableBuildResult BuildWindowTable(
        WindowPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        PhysicalToExecutionLoweringSession? session = null)
    {
        session ??= new PhysicalToExecutionLoweringSession(ResolveExecutionStrategies());
        var registrationsResult = ResolveSupportedWindowRegistrations(pipeline.Registrations);
        if (!registrationsResult.Supported)
            return TableBuildResult.Unsupported(registrationsResult.UnsupportedReason);

        var windowSource = BuildWindowSource(
            pipeline.Source.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            session);
        if (!windowSource.Supported)
            return TableBuildResult.Unsupported(windowSource.UnsupportedReason);

        var sourceShape = windowSource.Source.Shape;
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var aggregateSourceFields = CreateWindowAggregateSourceFieldLookup(pipeline.Source.Source);
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            sourceLookup);
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultShape = postOperationProjection.WorkingShape;
        var source = windowSource.Source.Variable;
        var sourceSetup = windowSource.Source.Setup;
        var sourceRows = windowSource.Source.Rows;
        var resultTable = postOperationProjection.WorkingTable;
        var buffer = CreateWindowRowsBufferVariable(
            $"{resultTableName}WindowRows",
            source,
            windowSource.Source.GeneratedRowShape);
        var windowIndex = new ExecutionVariable("windowIndex", typeof(int));
        var rowAccessMode = ResolveWindowRowAccessMode(sourceShape);
        var materialization = CreateWindowMaterialization(new WindowMaterializationContext(
            pipeline.Source,
            sourceRows,
            buffer,
            source,
            sourceShape,
            rowAccessMode,
            sourceLookup,
            windowSource.Source.GeneratedRowShape));

        if (!materialization.Supported)
            return TableBuildResult.Unsupported(materialization.UnsupportedReason);

        var qualifyTopRankPlan = CreateWindowQualifyTopRankPlan(
            pipeline.QualifyPredicate,
            registrationsResult.Value);

        var computations = CreateWindowComputations(
            registrationsResult.Value,
            buffer,
            source,
            rowAccessMode,
            sourceLookup,
            aggregateSourceFields,
            resultTableName,
            qualifyTopRankPlan.UpperBounds);

        if (!computations.Supported)
            return TableBuildResult.Unsupported(computations.UnsupportedReason);

        var windowResults = computations.Value.ToDictionary(
            computation => computation.Registration!.WindowIndex,
            computation => computation.Results);

        var preserveWindowAppendContexts = ShouldPreserveWindowAppendContexts(pipeline) || !string.Equals(resultTableName, "result", StringComparison.Ordinal);
        var appendRow = CreateWindowAppendRow(
            resultTable,
            resultShape,
            postOperationProjection.MaterializedFields,
            sourceLookup,
            aggregateSourceFields,
            windowResults,
            windowIndex,
            preserveWindowAppendContexts);

        if (!appendRow.Supported)
            return TableBuildResult.Unsupported(appendRow.UnsupportedReason);

        var appendBlock = CreateWindowAppendBlock(
            qualifyTopRankPlan.Predicate,
            appendRow.Value,
            sourceLookup,
            aggregateSourceFields,
            windowResults,
            windowIndex);

        if (!appendBlock.Supported)
            return TableBuildResult.Unsupported(appendBlock.UnsupportedReason);

        var windowKernelNodes = CreateWindowKernelPlanNodes(computations.Value);
        var nodes = new List<ExecutionNode>(sourceSetup.Count + pipeline.PostOperations.Count + windowKernelNodes.Count + 4);
        nodes.AddRange(sourceSetup);
        nodes.Add(materialization.Value);
        nodes.AddRange(windowKernelNodes);
        nodes.Add(CreateTable(
            resultTable,
            resultShape,
            preserveWindowAppendContexts
                ? ExecutionCapacityHintCandidates.CreateCollectionCountCandidate(resultTable, buffer)
                : null));
        nodes.Add(new ExecutionForEachIndexed(source, windowIndex, buffer, rowAccessMode, appendBlock.Value));

        return CompleteTableBuild(
            [..windowSource.Source.Shapes, ..postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection);
    }

    private static ExecutionVariable CreateWindowRowsBufferVariable(
        string name,
        ExecutionVariable source,
        GeneratedRowShape? generatedRowShape)
    {
        return string.IsNullOrWhiteSpace(generatedRowShape?.TypeName)
            ? new ExecutionVariable(name, CreateWindowBufferType(source))
            : CreateMaterializedRowsBufferVariable(name, generatedRowShape);
    }

    private static Type CreateWindowBufferType(ExecutionVariable source)
    {
        return string.IsNullOrWhiteSpace(source.GeneratedRowTypeName)
            ? typeof(List<>).MakeGenericType(source.Type)
            : typeof(object);
    }

    private static bool ShouldPreserveWindowAppendContexts(WindowPipeline pipeline)
    {
        return pipeline.QualifyPredicate != null ||
               pipeline.PostOperations.Count != 0 ||
               pipeline.Project.IsDistinct;
    }

}
