using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputeRankingWindow : ExecutionNode
{
    public ExecutionComputeRankingWindow(
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression? partitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionRankingWindowFunction function,
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeyArray = null,
        ExecutionWindowKeyArray? orderKeyArray = null,
        ExecutionWindowPartitionSet? partitions = null,
        ExecutionWindowPartitionSet? sortedPartitions = null,
        long? qualifyUpperBound = null)
    {
        Buffer = buffer;
        Item = item;
        RowAccessMode = rowAccessMode;
        PartitionKey = partitionKey;
        OrderKeys = ExecutionIrCollections.Freeze(orderKeys);
        Function = function;
        Results = results;
        PartitionKeyArray = partitionKeyArray;
        OrderKeyArray = orderKeyArray;
        Partitions = partitions;
        SortedPartitions = sortedPartitions;
        QualifyUpperBound = qualifyUpperBound;
    }

    public ExecutionVariable Buffer { get; init; }
    public ExecutionVariable Item { get; init; }
    public ExecutionRowAccessMode RowAccessMode { get; init; }
    public ExecutionExpression? PartitionKey { get; init; }
    public IReadOnlyList<ExecutionWindowOrderKey> OrderKeys { get; init; }
    public ExecutionRankingWindowFunction Function { get; init; }
    public ExecutionVariable Results { get; init; }
    public ExecutionWindowKeyArray? PartitionKeyArray { get; init; }
    public ExecutionWindowKeyArray? OrderKeyArray { get; init; }
    public ExecutionWindowPartitionSet? Partitions { get; init; }
    public ExecutionWindowPartitionSet? SortedPartitions { get; init; }
    public long? QualifyUpperBound { get; init; }
}
