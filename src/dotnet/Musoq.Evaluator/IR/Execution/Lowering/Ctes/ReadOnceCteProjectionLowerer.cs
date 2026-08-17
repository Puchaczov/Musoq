using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed class ReadOnceCteProjectionLowerer(
    Func<PhysicalNode, PhysicalNode> unwrapSingleStatement,
    Func<PhysicalNode, CteSupportedPipeline?> decomposeSupportedPipeline,
    Func<PhysicalProjectNode, PhysicalFilterNode?, PhysicalProjectNode, PhysicalCteRefNode, FinalJoinProjectionRewrite?> rewriteFinalJoinProjection,
    Func<PhysicalNode, bool> canInlineFinalProjectionSource)
{
    public ReadOnceCteProjectionFusion? TryCreateFusion(
        PhysicalCteDefinition[] definitions,
        CteSupportedPipeline finalPipeline,
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
            var fusion = TryFuseProjection(
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
                return CreateFusion(
                    fusion.DefinitionIndex,
                    fusion.Pipeline,
                    fusedDefinitionIndexes);
            }

            var upstreamIndex = FindCteDefinitionIndex(definitions, upstreamCteRef.CteName);
            if (upstreamIndex != fusion.DefinitionIndex - 1)
            {
                return CreateFusion(
                    fusion.DefinitionIndex,
                    fusion.Pipeline,
                    fusedDefinitionIndexes);
            }

            currentCteRef = upstreamCteRef;
            currentProject = fusion.Pipeline.Project;
            currentFilter = fusion.Pipeline.Filter;
        }
    }

    public static IReadOnlyList<ExecutionNode> CreateCandidates(
        IReadOnlyList<int> fusedCteIndexes,
        IReadOnlyList<ExecutionNode> bodyNodes)
    {
        return fusedCteIndexes
            .Select((relatedIndex, index) => (ExecutionNode)new ExecutionCteReadOnceFusionCandidate(
                relatedIndex,
                index == fusedCteIndexes.Count - 1 ? new ExecutionBlock(bodyNodes) : new ExecutionBlock([])))
            .ToArray();
    }

    public static PhysicalFilterNode? CombineProducerAndFinalFilters(
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

    private ReadOnceCteProjectionStep? TryFuseProjection(
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
        var producerPipeline = decomposeSupportedPipeline(unwrapSingleStatement(producer.Plan));
        if (producerPipeline == null ||
            producerPipeline.PostOperations.Count != 0 ||
            !CanInlineReadOnceProjectionSource(producerPipeline.Source) ||
            !IsPipelineDeterministic(producerPipeline))
        {
            return null;
        }

        var rewrite = rewriteFinalJoinProjection(
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

    private bool CanInlineReadOnceProjectionSource(PhysicalNode source)
    {
        return source is PhysicalSchemaScanNode or PhysicalCteRefNode ||
               canInlineFinalProjectionSource(source);
    }

    private static bool IsPipelineDeterministic(CteSupportedPipeline pipeline)
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

    private static ReadOnceCteProjectionFusion CreateFusion(
        int rootDefinitionIndex,
        CteSupportedPipeline pipeline,
        List<int> fusedDefinitionIndexes)
    {
        fusedDefinitionIndexes.Sort();
        return new ReadOnceCteProjectionFusion(
            rootDefinitionIndex,
            pipeline,
            fusedDefinitionIndexes.ToArray());
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
}
