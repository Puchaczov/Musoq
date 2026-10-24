using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    private static ExecutionExpression? WindowExpressionAt(ExecutionNode node, int index) => node switch
    {
        ExecutionComputeRankingWindow ranking => WindowPrefixAt(ranking.PartitionKey, ranking.OrderKeys, index),
        ExecutionComputeOffsetWindow offset => WindowPrefixAt(offset.PartitionKey, offset.OrderKeys, index) ??
            OffsetTailAt(offset, index - offset.OrderKeys.Count - (offset.PartitionKey is null ? 0 : 1)),
        ExecutionComputePluginWindow plugin => WindowPrefixAt(plugin.PartitionKey, plugin.OrderKeys, index) ??
            PluginTailAt(plugin, index - plugin.OrderKeys.Count - (plugin.PartitionKey is null ? 0 : 1)),
        ExecutionWindowAggregateKernel kernel => WindowPrefixAt(kernel.PartitionKey, kernel.OrderKeys, index) ??
            KernelTailAt(kernel, index - kernel.OrderKeys.Count - (kernel.PartitionKey is null ? 0 : 1)),
        _ => null
    };

    private static ExecutionExpression? WindowPrefixAt(ExecutionExpression? partition,
        IReadOnlyList<ExecutionWindowOrderKey> keys, int index)
    {
        if (partition is not null)
        {
            if (index == 0)
                return partition;
            index--;
        }
        return index < keys.Count ? keys[index].Expression : null;
    }

    private static ExecutionExpression? OffsetTailAt(ExecutionComputeOffsetWindow node, int index) =>
        index switch { 0 => node.Value, 1 => node.Offset, 2 => node.DefaultValue, _ => null };

    private static ExecutionExpression? PluginTailAt(ExecutionComputePluginWindow node, int index) =>
        index == 0 ? node.Value : ExecutionExpressionChildren.At(node.Arguments, index - 1);

    private static ExecutionExpression? KernelTailAt(ExecutionWindowAggregateKernel node, int index)
    {
        if (index == 0)
            return node.Value;
        index--;
        if (index < node.LibraryArguments.Count)
            return node.LibraryArguments[index];
        return index == node.LibraryArguments.Count ? node.FilterPredicate : null;
    }
}
