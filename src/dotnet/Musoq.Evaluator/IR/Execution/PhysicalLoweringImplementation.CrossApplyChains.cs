using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Lowering.ProjectionAndApply;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ApplyChainBuildResult BuildCrossApplyChain(
        PhysicalNestedLoopApplyNode apply,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> inheritedSourceLookup,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        var sourceCollector = new ApplyChainSourceCollector();
        if (!sourceCollector.TryCollectCrossApplySources(apply, out var physicalSources))
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
                sourceRowsScope,
                scope);
            if (!source.IsBuilt)
                return ApplyChainBuildResult.Unsupported(source.UnsupportedReason);

            var joinSource = physicalSource.WithOrdinality
                ? AddApplyOrdinalityAccess(source.Source)
                : source.Source;
            var guardResult = ApplyPredicateGuardLoweringService.Lower(
                physicalSource.ApplyPredicateMovementPlans,
                sourceLookup);
            var loweredPlans = joinSource.LoweredApplyPredicateMovementPlans
                .Concat(guardResult.LoweredPlans)
                .DistinctBy(static plan => plan.MovementId, StringComparer.Ordinal)
                .ToArray();
            sources.Add(joinSource with
            {
                ApplyPredicateMovementPlans = physicalSource.ApplyPredicateMovementPlans,
                ApplyGuardNodes = guardResult.GuardNodes,
                LoweredApplyPredicateMovementPlans = loweredPlans
            });
            currentSchemaFromIndex += source.Source.SchemaSourceCount;
            sourceLookup = JoinSourceLookupBuilder.Extend(sourceLookup, joinSource.Shape);
        }

        return ApplyChainBuildResult.Success(new ApplyChainSource(
            sources,
            sourceLookup,
            sources.SelectMany(static source => source.Shapes).ToArray()));
    }

    private TableBuildResult BuildCrossApplyChainTable(
        ApplyChainSource chain,
        CteSupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        LoweringScope scope)
    {
        var projection = CreatePostOperationProjection(
            resultTableName,
            resultShapeName,
            pipeline.Project.Fields,
            pipeline.PostOperations,
            chain.SourceLookup);
        if (!projection.IsBuilt)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var postOperationProjection = projection.Value
            ?? throw new InvalidOperationException("Supported post-operation projection requires projection data.");

        var resultTable = postOperationProjection.WorkingTable;
        var resultShape = postOperationProjection.WorkingShape;
        var appendRow = CreateAppendRow(resultTable, resultShape, postOperationProjection.MaterializedFields, chain.SourceLookup);
        var loweredPlans = chain.Sources
            .SelectMany(static source => source.LoweredApplyPredicateMovementPlans)
            .ToArray();
        var residualFilter = ApplyPredicateGuardLoweringService.RemoveLoweredPredicates(
            pipeline.Filter,
            loweredPlans);
        var loopBody = CreateLoopBody(
            residualFilter,
            CreateOutputAppend(appendRow, scope),
            chain.SourceLookup);
        var hoistedSetup = CreateCrossApplyChainHoistedSetup(chain.Sources);
        var nodes = new List<ExecutionNode>(hoistedSetup.Count + 2);

        nodes.AddRange(hoistedSetup);
        AddOutputTableCreation(nodes, resultTable, resultShape, scope);
        nodes.Add(CreateCrossApplyChainLoop(chain.Sources, 0, loopBody));

        return CompleteOutputTableBuild(
            scope,
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
        var body = new ExecutionBlock([..nextSource.ApplyGuardNodes, ..nextSetup, nextLoop]);

        return CreateApplySourceLoop(source, body);
    }
}
