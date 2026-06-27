using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildReadOnceCteProjectionTable(
        PhysicalCteNode cte,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyCollection<string> cteDefinitionNames,
        Dictionary<string, GeneratedRowShape> cteShapesByName,
        Dictionary<string, int> schemaFromIndexes,
        IReadOnlyList<ParallelCteLevel>? parallelLevels,
        CteDefinitionPruningPlan pruningPlan)
    {
        if (parallelLevels != null || cte.Definitions.Length == 0)
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(cte.Query));
        if (finalPipeline is not { Source: PhysicalCteRefNode finalCteRef } ||
            finalPipeline.PostOperations.Count != 0)
        {
            return null;
        }

        var strategy = ExecutionStrategies.GetCteStrategy(cte);
        var fusion = TryCreateReadOnceCteProjectionFusion(cte.Definitions, finalPipeline, finalCteRef, strategy);
        if (fusion == null)
            return null;

        var prefix = BuildCteDefinitionPrefix(
            cte,
            cte.Definitions,
            fusion.RootDefinitionIndex,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            pruningPlan,
            applySidecarIndexes: false);
        if (!prefix.Supported)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var rootDefinition = cte.Definitions[fusion.RootDefinitionIndex];
        var result = BuildTable(
            fusion.Pipeline,
            resultTableName,
            resultShapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes[rootDefinition.Name]);
        if (!result.Supported)
            return result;

        return TableBuildResult.Success(
            [..prefix.Shapes, ..result.Shapes],
            [..prefix.Nodes, ..CreateReadOnceCteCandidates(fusion.FusedDefinitionIndexes, result.Nodes)],
            result.Table,
            result.RowShape);
    }

    private static ReadOnceCteProjectionFusion? TryCreateReadOnceCteProjectionFusion(
        PhysicalCteDefinition[] definitions,
        SupportedPipeline finalPipeline,
        PhysicalCteRefNode finalCteRef,
        CteStrategyDecision strategy)
    {
        var producerIndex = FindCteDefinitionIndex(definitions, finalCteRef.CteName);
        if (producerIndex < 0 || producerIndex != definitions.Length - 1)
            return null;

        var currentCteRef = finalCteRef;
        var currentProject = finalPipeline.Project;
        var currentFilter = finalPipeline.Filter;
        var fusedDefinitionIndexes = new List<int>();

        while (true)
        {
            var fusion = TryFuseReadOnceCteProjection(
                definitions,
                currentCteRef,
                currentProject,
                currentFilter,
                strategy);
            if (fusion == null)
                return null;

            fusedDefinitionIndexes.Add(fusion.DefinitionIndex);

            if (fusion.Pipeline.Source is not PhysicalCteRefNode upstreamCteRef ||
                !strategy.CanFuseReadOnce(upstreamCteRef.CteName))
            {
                return CreateReadOnceCteProjectionFusion(
                    fusion.DefinitionIndex,
                    fusion.Pipeline,
                    fusedDefinitionIndexes);
            }

            var upstreamIndex = FindCteDefinitionIndex(definitions, upstreamCteRef.CteName);
            if (upstreamIndex != fusion.DefinitionIndex - 1)
            {
                return CreateReadOnceCteProjectionFusion(
                    fusion.DefinitionIndex,
                    fusion.Pipeline,
                    fusedDefinitionIndexes);
            }

            currentCteRef = upstreamCteRef;
            currentProject = fusion.Pipeline.Project;
            currentFilter = fusion.Pipeline.Filter;
        }
    }

    private static ReadOnceCteProjectionStep? TryFuseReadOnceCteProjection(
        PhysicalCteDefinition[] definitions,
        PhysicalCteRefNode cteRef,
        PhysicalProjectNode project,
        PhysicalFilterNode? filter,
        CteStrategyDecision strategy)
    {
        var producerIndex = FindCteDefinitionIndex(definitions, cteRef.CteName);
        if (producerIndex < 0 || !strategy.CanFuseReadOnce(cteRef.CteName))
            return null;

        var producer = definitions[producerIndex];
        var producerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(producer.Plan));
        if (producerPipeline == null ||
            producerPipeline.PostOperations.Count != 0 ||
            !CanInlineReadOnceCteProjectionSource(producerPipeline.Source) ||
            !IsReadOnceCtePipelineDeterministic(producerPipeline))
        {
            return null;
        }

        var rewrite = RewriteFinalJoinProjection(
            project,
            filter,
            producerPipeline.Project,
            cteRef);
        if (rewrite == null)
            return null;

        var combinedFilter = CombineProducerAndFinalFilters(
            producerPipeline.Filter,
            rewrite.Filter,
            producerPipeline.Source);
        if (!AreProjectedFieldsDeterministic(rewrite.Project) || !IsFilterDeterministic(combinedFilter))
            return null;

        return new ReadOnceCteProjectionStep(
            producerIndex,
            producerPipeline with
            {
                Project = rewrite.Project,
                Filter = combinedFilter
            });
    }

    private static bool IsReadOnceCtePipelineDeterministic(SupportedPipeline pipeline)
    {
        return AreProjectedFieldsDeterministic(pipeline.Project) &&
               IsFilterDeterministic(pipeline.Filter);
    }

    private static bool AreProjectedFieldsDeterministic(PhysicalProjectNode project)
    {
        foreach (var field in project.Fields)
        {
            if (!IrExpressionDeterminism.IsDeterministic(field.Expression))
                return false;
        }

        return true;
    }

    private static bool IsFilterDeterministic(PhysicalFilterNode? filter)
    {
        return filter == null || IrExpressionDeterminism.IsDeterministic(filter.Predicate);
    }

    private static ReadOnceCteProjectionFusion CreateReadOnceCteProjectionFusion(
        int rootDefinitionIndex,
        SupportedPipeline pipeline,
        List<int> fusedDefinitionIndexes)
    {
        fusedDefinitionIndexes.Sort();
        return new ReadOnceCteProjectionFusion(
            rootDefinitionIndex,
            pipeline,
            fusedDefinitionIndexes.ToArray());
    }

    private static IReadOnlyList<ExecutionNode> CreateReadOnceCteCandidates(
        IReadOnlyList<int> fusedCteIndexes,
        IReadOnlyList<ExecutionNode> bodyNodes)
    {
        return fusedCteIndexes
            .Select((relatedIndex, index) => (ExecutionNode)new ExecutionCteReadOnceFusionCandidate(
                relatedIndex,
                index == fusedCteIndexes.Count - 1 ? new ExecutionBlock(bodyNodes) : new ExecutionBlock([])))
            .ToArray();
    }

    private static PhysicalFilterNode? CombineProducerAndFinalFilters(
        PhysicalFilterNode? producerFilter,
        PhysicalFilterNode? finalFilter,
        PhysicalNode source)
    {
        if (producerFilter == null)
            return finalFilter;

        if (finalFilter == null)
            return producerFilter;

        return new PhysicalFilterNode(
            new BinaryOp(
                BinaryOpKind.And,
                producerFilter.Predicate,
                finalFilter.Predicate,
                typeof(bool)),
            source);
    }

    private static int FindCteDefinitionIndex(
        PhysicalCteDefinition[] definitions,
        string name)
    {
        for (var index = 0; index < definitions.Length; index++)
        {
            if (string.Equals(definitions[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }

    private static bool CanInlineReadOnceCteProjectionSource(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode or PhysicalCteRefNode ||
               CanInlineFinalProjectionSource(source);
    }

    private sealed record ReadOnceCteProjectionFusion(
        int RootDefinitionIndex,
        SupportedPipeline Pipeline,
        int[] FusedDefinitionIndexes);

    private sealed record ReadOnceCteProjectionStep(
        int DefinitionIndex,
        SupportedPipeline Pipeline);
}
