using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteSourceBackedSiblingFusion
{
    public static ExecutionFusedCteProducer? TryRewrite(
        List<ExecutionNode> builtNodes,
        List<RowShape> shapes,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications,
        IReadOnlyDictionary<string, SourceInteractionPlan> sourceInteractionPlans,
        ExecutionFusedCteProducer producer)
    {
        if (producer.Outputs.Any(static output => output.StoreRows) ||
            !CteSourceBackedMaterializationDiscovery.TryFindStoredRowsLoop(
                producer.Body,
                out var producerLoopIndex,
                out var producerLoop,
                out var sourceTableIndex) ||
            !CteSourceBackedMaterializationDiscovery.TryResolveCteName(cteIndexes, sourceTableIndex, out var sourceCteName) ||
            !classifications.TryGetValue(sourceCteName, out var classification) ||
            classification.ReferenceCount != producer.Outputs.Count ||
            !CteSourceBackedMaterializationDiscovery.TryExtractSourceMaterialization(
                builtNodes,
                sourceTableIndex,
                sourceInteractionPlans,
                out var materialization) ||
            !CteSourceBackedExpressionRewriter.TryCreateSourceBackedLoop(materialization, producerLoop, out var sourceLoop))
        {
            return null;
        }

        var bodyNodes = producer.Body.Nodes.ToArray();
        bodyNodes[producerLoopIndex] = sourceLoop;
        for (var index = 0; index < bodyNodes.Length; index++)
        {
            bodyNodes[index] = CteSourceBackedCapacityHintRewriter.RewriteNode(
                bodyNodes[index],
                sourceTableIndex,
                materialization.Loop.Source);
        }

        builtNodes.RemoveRange(
            materialization.CreateTableIndex,
            materialization.StoreTableIndex - materialization.CreateTableIndex + 1);
        builtNodes.Insert(
            materialization.CreateTableIndex,
            new ExecutionCteReadOnceFusionCandidate(sourceTableIndex, new ExecutionBlock([])));
        shapes.RemoveAll(shape => shape is GeneratedRowShape generated &&
                                  string.Equals(generated.TypeName, materialization.CreateTable.RowShape.TypeName, StringComparison.Ordinal));

        return producer with { Body = new ExecutionBlock(bodyNodes) };
    }
}
