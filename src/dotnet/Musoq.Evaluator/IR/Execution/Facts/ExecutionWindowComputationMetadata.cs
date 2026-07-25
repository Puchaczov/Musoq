using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal sealed record ExecutionWindowComputationMetadata
{
    public ExecutionWindowComputationMetadata(
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression? partitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeyArray,
        ExecutionWindowKeyArray? orderKeyArray,
        ExecutionWindowPartitionSet? partitions,
        ExecutionWindowPartitionSet? sortedPartitions)
    {
        Buffer = buffer;
        Item = item;
        RowAccessMode = rowAccessMode;
        PartitionKey = partitionKey;
        OrderKeys = ExecutionIrCollections.Freeze(orderKeys);
        Results = results;
        PartitionKeyArray = partitionKeyArray;
        OrderKeyArray = orderKeyArray;
        Partitions = partitions;
        SortedPartitions = sortedPartitions;
    }

    public ExecutionVariable Buffer { get; init; }

    public ExecutionVariable Item { get; init; }

    public ExecutionRowAccessMode RowAccessMode { get; init; }

    public ExecutionExpression? PartitionKey { get; init; }

    public IReadOnlyList<ExecutionWindowOrderKey> OrderKeys { get; init; }

    public ExecutionVariable Results { get; init; }

    public ExecutionWindowKeyArray? PartitionKeyArray { get; init; }

    public ExecutionWindowKeyArray? OrderKeyArray { get; init; }

    public ExecutionWindowPartitionSet? Partitions { get; init; }

    public ExecutionWindowPartitionSet? SortedPartitions { get; init; }
}
