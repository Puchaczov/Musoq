using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildSingleUseSidecarJoinChainTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            multiStatement.Statements.Length < 2)
        {
            return null;
        }

        var classifications = ClassifyMultiStatementCteReferences(multiStatement, indexes);
        var stages = new List<SidecarJoinPipelineStage>(multiStatement.Statements.Length);
        string? previousOutputName = null;

        for (var index = 0; index < multiStatement.Statements.Length; index++)
        {
            var pipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[index]));
            if (pipeline == null)
                return null;

            var isFinalStatement = index == multiStatement.Statements.Length - 1;
            string? outputName = null;

            if (!isFinalStatement)
            {
                outputName = ResolveStatementCteName(index, indexes);
                if (string.IsNullOrWhiteSpace(outputName) ||
                    !CanFuseReadOnceCte(outputName, classifications))
                {
                    return null;
                }
            }

            stages.Add(new SidecarJoinPipelineStage(pipeline, previousOutputName, outputName));
            previousOutputName = outputName;
        }

        return TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName) ??
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
        CteDefinitionPruningPlan pruningPlan)
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
        if (!CanFuseReadOnceCte(producer.Name, classifications) ||
            !TryCreateCteSidecarJoinPipelineStages(
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
            applySidecarIndexes: true);
        if (!prefix.Supported)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var pipeline = TryBuildSidecarJoinPipelineTable(
            stages,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes[producer.Name]);
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

    private bool TryCreateCteSidecarJoinPipelineStages(
        PhysicalCteDefinition producer,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        SupportedPipeline finalPipeline,
        out IReadOnlyList<SidecarJoinPipelineStage> stages)
    {
        stages = [];
        var producerPlan = UnwrapSingleStatement(producer.Plan);
        var collected = new List<SidecarJoinPipelineStage>();

        if (producerPlan is PhysicalMultiStatementNode producerMultiStatement)
        {
            var producerDefinitionIndex = cteIndexes[producer.Name];
            var producerIndexes = CreateMultiStatementIndexes(
                producerMultiStatement,
                cteIndexes,
                cteShapesByName,
                CreateCteTableName(producerDefinitionIndex, cteDefinitionNames));
            string? previousOutputName = null;

            for (var index = 0; index < producerMultiStatement.Statements.Length; index++)
            {
                var pipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(producerMultiStatement.Statements[index]));
                if (pipeline == null)
                    return false;

                var outputName = index == producerMultiStatement.Statements.Length - 1
                    ? producer.Name
                    : ResolveStatementCteName(index, producerIndexes);
                if (string.IsNullOrWhiteSpace(outputName))
                    return false;

                collected.Add(new SidecarJoinPipelineStage(pipeline, previousOutputName, outputName));
                previousOutputName = outputName;
            }
        }
        else
        {
            return false;
        }

        collected.Add(new SidecarJoinPipelineStage(finalPipeline, producer.Name, null));
        stages = collected;
        return true;
    }

    private sealed record SidecarJoinPipelineStage(
        SupportedPipeline Pipeline,
        string? ExpectedInputCteName,
        string? OutputCteName);

    private abstract record SidecarJoinRuntimeOperation(
        IReadOnlySet<string> RequiredAliases,
        int Ordinal);

    private sealed record SidecarJoinRuntimeStep(
        PhysicalHashJoinNode Join,
        CteSidecarIndexSpec Sidecar,
        JoinSource Build,
        ExecutionVariable Index,
        ExecutionVariable? Matches,
        IrExpression[] ProbeKeys,
        IrExpression? Residual,
        PhysicalFilterNode? Filter,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        IReadOnlySet<string> RequiredAliases,
        IReadOnlySet<string> IntroducedAliases,
        int Ordinal) : SidecarJoinRuntimeOperation(RequiredAliases, Ordinal);

    private sealed record SidecarJoinRuntimeGuard(
        IrExpression Predicate,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        IReadOnlySet<string> RequiredAliases,
        int Ordinal) : SidecarJoinRuntimeOperation(RequiredAliases, Ordinal);
}
