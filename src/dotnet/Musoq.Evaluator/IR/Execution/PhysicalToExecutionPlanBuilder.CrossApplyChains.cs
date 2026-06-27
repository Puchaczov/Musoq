using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ApplyChainBuildResult BuildCrossApplyChain(
        PhysicalNestedLoopApplyNode apply,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> inheritedSourceLookup,
        string? sourceRowsScope)
    {
        var physicalSources = new List<ApplyChainPhysicalSource>();
        if (!CollectCrossApplySources(apply, physicalSources))
        {
            return ApplyChainBuildResult.Unsupported(
                "Execution IR cross-apply chain streaming only supports all-cross APPLY chains.");
        }

        var sources = new List<JoinSource>(physicalSources.Count);
        var sourceLookup = new Dictionary<string, RowShape>(inheritedSourceLookup, StringComparer.OrdinalIgnoreCase);
        var currentSchemaFromIndex = schemaFromIndex;

        foreach (var physicalSource in physicalSources)
        {
            var source = BuildApplySource(
                physicalSource.Source,
                cteIndexes,
                cteShapesByName,
                currentSchemaFromIndex,
                sourceLookup,
                sourceRowsScope);
            if (!source.Supported)
                return ApplyChainBuildResult.Unsupported(source.UnsupportedReason);

            var joinSource = physicalSource.WithOrdinality
                ? AddApplyOrdinalityAccess(source.Source)
                : source.Source;
            sources.Add(joinSource);
            currentSchemaFromIndex += source.Source.SchemaSourceCount;
            sourceLookup = new Dictionary<string, RowShape>(
                RowShapeLookup.CreateSourceShapeLookup(sourceLookup, joinSource.Shape),
                StringComparer.OrdinalIgnoreCase);
        }

        return ApplyChainBuildResult.Success(new ApplyChainSource(
            sources,
            sourceLookup,
            sources.SelectMany(static source => source.Shapes).ToArray()));
    }

    private static bool CollectCrossApplySources(PhysicalNode source, List<ApplyChainPhysicalSource> sources)
    {
        if (source is not PhysicalNestedLoopApplyNode apply)
        {
            sources.Add(new ApplyChainPhysicalSource(source, false));
            return true;
        }

        if (apply.Kind != ApplyKind.Cross)
            return false;

        if (!CollectCrossApplySources(apply.Left, sources))
            return false;

        sources.Add(new ApplyChainPhysicalSource(apply.Right, apply.WithOrdinality));
        return true;
    }

    private TableBuildResult BuildCrossApplyChainTable(
        ApplyChainSource chain,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName)
    {
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            chain.SourceLookup);
        if (!projection.Supported)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultTable = postOperationProjection.WorkingTable;
        var resultShape = postOperationProjection.WorkingShape;
        var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, chain.SourceLookup);
        var loopBody = CreateLoopBody(pipeline.Filter, appendRow, chain.SourceLookup);
        var hoistedSetup = CreateCrossApplyChainHoistedSetup(chain.Sources);
        var nodes = new List<ExecutionNode>(hoistedSetup.Count + 2);

        nodes.AddRange(hoistedSetup);
        nodes.Add(CreateTable(resultTable, resultShape));
        nodes.Add(CreateCrossApplyChainLoop(chain.Sources, 0, loopBody));

        return CompleteTableBuild(
            [..chain.Shapes, ..postOperationProjection.Shapes],
            nodes,
            resultTable,
            resultShape,
            postOperationProjection.PostOperations,
            pipeline.Project.IsDistinct,
            postOperationProjection.FinalProjection);
    }

    private static List<ExecutionNode> CreateCrossApplyChainHoistedSetup(IReadOnlyList<JoinSource> sources)
    {
        var nodes = new List<ExecutionNode>();
        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            if (index > 0 && !source.CanReuseSetupAcrossApplyRows)
                continue;

            nodes.AddRange(source.Setup);
        }

        return nodes;
    }

    private static ExecutionNode CreateCrossApplyChainLoop(
        IReadOnlyList<JoinSource> sources,
        int sourceIndex,
        ExecutionBlock innermostBody)
    {
        var source = sources[sourceIndex];
        if (sourceIndex == sources.Count - 1)
            return CreateApplySourceLoop(source, innermostBody);

        var nextSource = sources[sourceIndex + 1];
        var nextLoop = CreateCrossApplyChainLoop(sources, sourceIndex + 1, innermostBody);
        IReadOnlyList<ExecutionNode> nextSetup = nextSource.CanReuseSetupAcrossApplyRows
            ? []
            : nextSource.Setup;
        var body = new ExecutionBlock([..nextSetup, nextLoop]);

        return CreateApplySourceLoop(source, body);
    }
}
