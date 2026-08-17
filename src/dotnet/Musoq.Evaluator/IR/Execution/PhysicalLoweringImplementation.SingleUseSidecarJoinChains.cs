using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult? TryBuildSingleUseSidecarJoinChainTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            scope.SuppressSidecarJoinPipeline ||
            multiStatement.Statements.Length < 2)
        {
            return null;
        }

        var lowerer = CreateSidecarJoinCteLowerer();
        if (!lowerer.TryCreateSingleUseChainStages(multiStatement, indexes, out var stages))
            return null;

        if (!CanUseStreamingSidecarJoinPipeline(stages))
        {
            return BuildDelegatedSidecarEnabledMultiStatementTable(
                multiStatement,
                resultTableName,
                resultShapeName,
                indexes,
                scopeAggregateVariables,
                scope);
        }

        return TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope) ??
            TableBuildResult.Unsupported(
                "Execution IR CTE sidecar join-chain lowering selected a sidecar pipeline but could not materialize it; the lowerer must not fall back to regular multi-statement lowering silently.");
    }

    private TableBuildResult BuildDelegatedSidecarEnabledMultiStatementTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        return BuildMultiStatementTable(
            multiStatement,
            resultTableName,
            resultShapeName,
            indexes,
            scopeAggregateVariables,
            scope.WithSidecarJoinPipelineSuppressed());
    }

    private bool CanUseStreamingSidecarJoinPipeline(IReadOnlyList<SidecarJoinPipelineStage> stages)
    {
        return stages
            .Select(AnalyzeSidecarJoinPipelineStage)
            .All(static analysis => analysis.Kind is
                SidecarJoinPipelineStageKind.Projection or
                SidecarJoinPipelineStageKind.IndexedHashJoin or
                SidecarJoinPipelineStageKind.IndexedKeySetJoin);
    }

    private SidecarJoinPipelineStageAnalysis AnalyzeSidecarJoinPipelineStage(SidecarJoinPipelineStage stage)
    {
        if (IsSidecarProjectionStage(stage))
            return new SidecarJoinPipelineStageAnalysis(
                SidecarJoinPipelineStageKind.Projection,
                "Projection over the previous sidecar pipeline output.");

        if (stage.Pipeline.Source is PhysicalHashJoinNode hashJoin &&
            !stage.Pipeline.Project.IsDistinct &&
            stage.Pipeline.PostOperations.Count == 0 &&
            TryResolveSidecarBuildCteRef(hashJoin, out _, out var sidecar) &&
            IsSupportedSidecarPipelineJoin(hashJoin, sidecar))
        {
            return new SidecarJoinPipelineStageAnalysis(
                sidecar.Kind == CteSidecarIndexKind.KeySet
                    ? SidecarJoinPipelineStageKind.IndexedKeySetJoin
                    : SidecarJoinPipelineStageKind.IndexedHashJoin,
                $"Indexed sidecar {sidecar.Kind} join.");
        }

        return new SidecarJoinPipelineStageAnalysis(
            ResolveDelegatedSidecarStageKind(stage.Pipeline.Source),
            "Delegated to standard Execution IR lowering under sidecar-enabled options.");
    }

    private static bool IsSidecarProjectionStage(SidecarJoinPipelineStage stage)
    {
        return stage.Pipeline.Source is PhysicalCteRefNode cteRef &&
               !stage.Pipeline.Project.IsDistinct &&
               stage.Pipeline.PostOperations.Count == 0 &&
               !string.IsNullOrWhiteSpace(stage.ExpectedInputCteName) &&
               string.Equals(cteRef.CteName, stage.ExpectedInputCteName, StringComparison.OrdinalIgnoreCase);
    }

    private static SidecarJoinPipelineStageKind ResolveDelegatedSidecarStageKind(PhysicalNode source)
    {
        return source switch
        {
            PhysicalNestedLoopApplyNode => SidecarJoinPipelineStageKind.Apply,
            PhysicalNestedLoopJoinNode { Kind: JoinKind.Cross } => SidecarJoinPipelineStageKind.CrossJoin,
            PhysicalNestedLoopJoinNode { Kind: JoinKind.AsofInner or JoinKind.AsofLeft } => SidecarJoinPipelineStageKind.AsOfJoin,
            PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode => SidecarJoinPipelineStageKind.StandardJoin,
            _ => SidecarJoinPipelineStageKind.StandardJoin
        };
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
        LoweringScope scope)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            scope.SuppressSidecarJoinPipeline ||
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
            scope);
        if (!prefix.IsBuilt)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        scope = prefix.UpdatedScope ?? scope;

        var pipeline = TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes[producer.Name],
            scope);
        if (pipeline == null)
        {
            return TableBuildResult.Unsupported(
                "Execution IR read-once CTE sidecar lowering selected a sidecar pipeline but could not materialize it; the lowerer must not fall back to regular CTE materialization silently.");
        }

        if (!pipeline.IsBuilt)
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
