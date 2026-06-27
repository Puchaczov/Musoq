using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class CteStrategyPlanner
{
    public static CteStrategyDecision Choose(PhysicalCteNode node)
    {
        var referenceCounts = CountCteReferences(node);
        var readOnceFusionCandidates = ResolveReadOnceFusionCandidates(node, referenceCounts);
        var definitions = new Dictionary<string, CteDefinitionStrategyDecision>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in node.Definitions)
        {
            referenceCounts.TryGetValue(definition.Name, out var referenceCount);
            var characteristics = ClassifyCteOutput(ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(definition.Plan));
            var fusionEligibility = readOnceFusionCandidates.Contains(definition.Name)
                ? CteFusionEligibility.ReadOnceCandidate
                : CteFusionEligibility.NotCandidate;
            definitions[definition.Name] = ChooseCteDefinitionStrategy(
                definition.Name,
                referenceCount,
                characteristics,
                fusionEligibility);
        }

        return new CteStrategyDecision(definitions);
    }

    private static CteDefinitionStrategyDecision ChooseCteDefinitionStrategy(
        string name,
        int referenceCount,
        CteOutputCharacteristics characteristics,
        CteFusionEligibility fusionEligibility)
    {
        if (referenceCount == 0)
        {
            return new CteDefinitionStrategyDecision(
                name,
                referenceCount,
                characteristics,
                CteDefinitionStrategyKind.Unreferenced,
                "CTE has no remaining references after logical rewrites, so no runtime reuse table is required.");
        }

        if (referenceCount > 1)
        {
            return new CteDefinitionStrategyDecision(
                name,
                referenceCount,
                characteristics,
                CteDefinitionStrategyKind.MaterializeReuse,
                $"CTE is referenced {referenceCount.ToString(CultureInfo.InvariantCulture)} times, so reuse requires a materialized table.");
        }

        if (fusionEligibility == CteFusionEligibility.ReadOnceCandidate)
        {
            return new CteDefinitionStrategyDecision(
                name,
                referenceCount,
                characteristics,
                CteDefinitionStrategyKind.FuseReadOnce,
                "Single-use CTE output is safe for read-once projection fusion when the downstream shape supports it.");
        }

        return new CteDefinitionStrategyDecision(
            name,
            referenceCount,
            characteristics,
            CteDefinitionStrategyKind.MaterializeSingleUse,
            CreateSingleUseCteMaterializationReason(characteristics));
    }


    private static HashSet<string> ResolveReadOnceFusionCandidates(
        PhysicalCteNode node,
        IReadOnlyDictionary<string, int> referenceCounts)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (node.Definitions.Length == 0)
            return candidates;

        var finalPipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(
            ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(node.Query));
        if (finalPipeline is not { Source: PhysicalCteRefNode finalCteRef } ||
            finalPipeline.PostOperations.Count != 0)
        {
            return candidates;
        }

        var producerIndex = FindCteDefinitionIndex(node.Definitions, finalCteRef.CteName);
        if (producerIndex < 0 || producerIndex != node.Definitions.Length - 1)
            return candidates;

        AddReadOnceFusionCandidates(
            node.Definitions,
            referenceCounts,
            finalCteRef.CteName,
            producerIndex,
            candidates);

        return candidates;
    }

    private static void AddReadOnceFusionCandidates(
        IReadOnlyList<PhysicalCteDefinition> definitions,
        IReadOnlyDictionary<string, int> referenceCounts,
        string cteName,
        int definitionIndex,
        ISet<string> candidates)
    {
        var currentName = cteName;
        var currentIndex = definitionIndex;

        while (TryAddReadOnceFusionCandidate(
                   definitions,
                   referenceCounts,
                   currentName,
                   currentIndex,
                   candidates,
                   out var upstreamCteRef,
                   out var upstreamIndex))
        {
            if (upstreamCteRef == null || upstreamIndex != currentIndex - 1)
                return;

            currentName = upstreamCteRef.CteName;
            currentIndex = upstreamIndex;
        }
    }

    private static bool TryAddReadOnceFusionCandidate(
        IReadOnlyList<PhysicalCteDefinition> definitions,
        IReadOnlyDictionary<string, int> referenceCounts,
        string cteName,
        int definitionIndex,
        ISet<string> candidates,
        out PhysicalCteRefNode? upstreamCteRef,
        out int upstreamIndex)
    {
        upstreamCteRef = null;
        upstreamIndex = -1;

        referenceCounts.TryGetValue(cteName, out var referenceCount);
        if (referenceCount != 1)
            return false;

        var producer = definitions[definitionIndex];
        var characteristics = ClassifyCteOutput(ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(producer.Plan));
        if (!CanPlanReadOnceFusion(characteristics))
            return false;

        var producerPipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(
            ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(producer.Plan));
        if (producerPipeline == null ||
            producerPipeline.PostOperations.Count != 0 ||
            !CanInlineReadOnceCteProjectionSource(producerPipeline.Source) ||
            !IsReadOnceCtePipelineDeterministic(producerPipeline))
        {
            return false;
        }

        candidates.Add(cteName);

        if (producerPipeline.Source is not PhysicalCteRefNode sourceCteRef)
            return true;

        upstreamIndex = FindCteDefinitionIndex(definitions, sourceCteRef.CteName);
        if (upstreamIndex < 0)
            return true;

        upstreamCteRef = sourceCteRef;
        return true;
    }


    private static int FindCteDefinitionIndex(
        IReadOnlyList<PhysicalCteDefinition> definitions,
        string name)
    {
        for (var index = 0; index < definitions.Count; index++)
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

    private static bool CanInlineFinalProjectionSource(PhysicalNode source)
    {
        return CanInlineFinalJoinProjectionSource(source) || IsPlainSchemaScanApplySource(source);
    }

    private static bool CanInlineFinalJoinProjectionSource(PhysicalNode source)
    {
        return source is PhysicalHashJoinNode or PhysicalNestedLoopJoinNode or PhysicalSortMergeJoinNode;
    }

    private static bool IsPlainSchemaScanApplySource(PhysicalNode source)
    {
        return source switch
        {
            PhysicalNestedLoopApplyNode apply =>
                IsPlainSchemaScanApplySource(apply.Left) &&
                IsPlainSchemaScanApplySource(apply.Right),
            PhysicalSchemaScanNode { Arguments.Length: 0 } => true,
            _ => false
        };
    }

    private static bool CanPlanReadOnceFusion(CteOutputCharacteristics characteristics)
    {
        return !characteristics.HasFlag(CteOutputCharacteristics.OrderSensitive) &&
               !characteristics.HasFlag(CteOutputCharacteristics.Window) &&
               !characteristics.HasFlag(CteOutputCharacteristics.SetOperation) &&
               !characteristics.HasFlag(CteOutputCharacteristics.SideEffectSensitive);
    }


    private static Dictionary<string, int> CountCteReferences(PhysicalCteNode node)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in node.Definitions)
            CountCteReferences(definition.Plan, counts);

        CountCteReferences(node.Query, counts);
        return counts;
    }

    private static void CountCteReferences(PhysicalNode node, IDictionary<string, int> counts)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            counts.TryGetValue(cteRef.CteName, out var count);
            counts[cteRef.CteName] = count + 1;
            return;
        }

        foreach (var child in node.Children)
            CountCteReferences(child, counts);
    }

    private static CteOutputCharacteristics ClassifyCteOutput(PhysicalNode node)
    {
        var characteristics = CteOutputCharacteristics.None;
        ClassifyCteOutput(node, ref characteristics);
        return characteristics;
    }

    private static void ClassifyCteOutput(PhysicalNode node, ref CteOutputCharacteristics characteristics)
    {
        if (node is PhysicalProjectNode project && !AreProjectedFieldsDeterministic(project))
            characteristics |= CteOutputCharacteristics.SideEffectSensitive;

        if (node is PhysicalFilterNode filter && !IsFilterDeterministic(filter))
            characteristics |= CteOutputCharacteristics.SideEffectSensitive;

        characteristics |= node switch
        {
            PhysicalSortNode or PhysicalSkipNode or PhysicalTakeNode or PhysicalTopNNode or PhysicalTopOffsetNode => CteOutputCharacteristics.OrderSensitive,
            PhysicalAggregateOnlyNode or PhysicalSingleKeyAggregateNode or PhysicalValueTupleAggregateNode => CteOutputCharacteristics.Aggregate,
            PhysicalWindowNode => CteOutputCharacteristics.Window,
            PhysicalSetOperationNode => CteOutputCharacteristics.SetOperation,
            PhysicalInterpretSourceNode or PhysicalAccessMethodSourceNode or PhysicalPropertySourceNode => CteOutputCharacteristics.SideEffectSensitive,
            _ => CteOutputCharacteristics.None
        };

        foreach (var child in node.Children)
            ClassifyCteOutput(child, ref characteristics);
    }

    private enum CteFusionEligibility
    {
        NotCandidate,
        ReadOnceCandidate
    }
}
