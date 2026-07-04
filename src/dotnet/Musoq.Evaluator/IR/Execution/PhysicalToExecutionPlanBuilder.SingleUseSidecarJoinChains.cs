using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildSingleUseSidecarJoinChainTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        PhysicalToExecutionLoweringSession session)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            multiStatement.Statements.Length < 2)
        {
            return null;
        }

        var lowerer = CreateSidecarJoinCteLowerer();
        if (!lowerer.TryCreateSingleUseChainStages(multiStatement, indexes, out var stages))
            return null;

        return TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            session: session) ??
            TableBuildResult.Unsupported(
                "Execution IR CTE sidecar join-chain lowering selected a sidecar pipeline but could not materialize it; the lowerer must not fall back to regular multi-statement lowering silently.");
    }

    private TableBuildResult? TryBuildReadOnceCteSidecarJoinTable(
        PhysicalCteNode cte,
        string resultTableName,
        string resultShapeName,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        IReadOnlyList<ParallelCteLevel>? parallelLevels,
        CteDefinitionPruningPlan pruningPlan,
        PhysicalToExecutionLoweringSession session)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            parallelLevels != null ||
            cte.Definitions.Length == 0)
        {
            return null;
        }

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(cte.Query));
        if (finalPipeline is not { Source: PhysicalHashJoinNode finalJoin } ||
            finalPipeline.Project.IsDistinct ||
            finalPipeline.PostOperations.Count != 0 ||
            !TryResolveSidecarBuildCteRef(finalJoin, out _, out _))
        {
            return null;
        }

        var producerIndex = cte.Definitions.Length - 1;
        var producer = cte.Definitions[producerIndex];
        var classifications = ClassifyCteReferences(cte);
        var lowerer = CreateSidecarJoinCteLowerer();
        if (!CanFuseReadOnceCte(producer.Name, classifications) ||
            !lowerer.TryCreateReadOnceCteStages(
                producer,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                finalPipeline,
                out var stages))
        {
            return null;
        }

        var prefix = BuildCteDefinitionPrefix(
            cte,
            cte.Definitions,
            producerIndex,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            pruningPlan,
            applySidecarIndexes: true,
            session);
        if (!prefix.Supported)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var pipeline = TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes[producer.Name],
            session);
        if (pipeline == null)
        {
            return TableBuildResult.Unsupported(
                "Execution IR read-once CTE sidecar lowering selected a sidecar pipeline but could not materialize it; the lowerer must not fall back to regular CTE materialization silently.");
        }

        if (!pipeline.Supported)
            return pipeline;

        return TableBuildResult.Success(
            [..prefix.Shapes, ..pipeline.Shapes],
            [..prefix.Nodes, new ExecutionCteReadOnceFusionCandidate(producerIndex, new ExecutionBlock(pipeline.Nodes))],
            pipeline.Table,
            pipeline.RowShape);
    }

    private static SidecarJoinCteLowerer CreateSidecarJoinCteLowerer()
    {
        return new SidecarJoinCteLowerer(
            UnwrapSingleStatement,
            DecomposeSupportedPipeline,
            ClassifyMultiStatementCteReferences,
            CanFuseReadOnceCte,
            ResolveStatementCteName,
            CreateMultiStatementIndexes,
            CreateCteTableName);
    }

}
