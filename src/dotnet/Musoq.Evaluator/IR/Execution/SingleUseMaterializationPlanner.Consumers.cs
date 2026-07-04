using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;
internal static partial class SingleUseMaterializationPlanner
{
    private static Dictionary<string, int> CountTrackedCteReferences(
        IEnumerable<PhysicalNode> nodes,
        IReadOnlySet<string> trackedNames)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
            CountTrackedCteReferences(node, trackedNames, counts);
        return counts;
    }

    private static void CountTrackedCteReferences(PhysicalNode node, IReadOnlySet<string> trackedNames, IDictionary<string, int> counts)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            if (trackedNames.Contains(cteRef.CteName))
            {
                counts.TryGetValue(cteRef.CteName, out var count);
                counts[cteRef.CteName] = count + 1;
            }

            return;
        }

        foreach (var child in node.Children)
            CountTrackedCteReferences(child, trackedNames, counts);
    }

    private static Dictionary<string, IReadOnlyList<SingleUseConsumerKind>> CollectConsumers(
        IReadOnlyList<PhysicalNode> nodes,
        IReadOnlySet<string> trackedNames)
    {
        var consumers = new Dictionary<string, List<SingleUseConsumerKind>>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
            CollectHashBuildConsumers(node, trackedNames, consumers);

        AddProjectionChainConsumers(nodes, trackedNames, consumers);

        return consumers.ToDictionary(
            static entry => entry.Key,
            static entry => (IReadOnlyList<SingleUseConsumerKind>)entry.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void CollectHashBuildConsumers(
        PhysicalNode node,
        IReadOnlySet<string> trackedNames,
        Dictionary<string, List<SingleUseConsumerKind>> consumers)
    {
        if (node is PhysicalHashJoinNode hashJoin)
        {
            AddHashBuildConsumer(hashJoin.Left, hashJoin, trackedNames, consumers);
            AddHashBuildConsumer(hashJoin.Right, hashJoin, trackedNames, consumers);
        }

        foreach (var child in node.Children)
            CollectHashBuildConsumers(child, trackedNames, consumers);
    }

    private static void AddHashBuildConsumer(
        PhysicalNode side,
        PhysicalHashJoinNode hashJoin,
        IReadOnlySet<string> trackedNames,
        Dictionary<string, List<SingleUseConsumerKind>> consumers)
    {
        if (side is not PhysicalCteRefNode cteRef ||
            !trackedNames.Contains(cteRef.CteName) ||
            !HashBuildAliasUsage.BuildKeysReferenceAlias(hashJoin, cteRef.Alias))
        {
            return;
        }

        AddConsumer(consumers, cteRef.CteName, SingleUseConsumerKind.HashBuild);
    }
    private static void AddProjectionChainConsumers(
        IReadOnlyList<PhysicalNode> nodes,
        IReadOnlySet<string> trackedNames,
        Dictionary<string, List<SingleUseConsumerKind>> consumers)
    {
        for (var index = 0; index < nodes.Count; index++)
            AddProjectionChainConsumer(
                nodes[index],
                trackedNames,
                consumers,
                index == nodes.Count - 1 ? SingleUseConsumerKind.FinalProjection : SingleUseConsumerKind.ProjectionChain);
    }

    private static void AddProjectionChainConsumer(PhysicalNode node, IReadOnlySet<string> trackedNames, Dictionary<string, List<SingleUseConsumerKind>> consumers, SingleUseConsumerKind consumerKind)
    {
        var unwrapped = ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(node);
        if (unwrapped is PhysicalMultiStatementNode { Statements.Length: > 0 } multiStatement)
            unwrapped = ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(multiStatement.Statements[^1]);

        var pipeline = ExecutionStrategyPipelineDecomposer.TryDecomposeSupportedPipeline(unwrapped);
        if (pipeline is not { Source: PhysicalCteRefNode cteRef } ||
            pipeline.PostOperations.Count != 0 ||
            !trackedNames.Contains(cteRef.CteName))
            return;

        AddConsumer(consumers, cteRef.CteName, consumerKind);
    }

    private static void AddConsumer(Dictionary<string, List<SingleUseConsumerKind>> consumers, string name, SingleUseConsumerKind consumer)
    {
        if (!consumers.TryGetValue(name, out var existing))
        {
            consumers[name] = [consumer];
            return;
        }

        existing.Add(consumer);
    }
}
