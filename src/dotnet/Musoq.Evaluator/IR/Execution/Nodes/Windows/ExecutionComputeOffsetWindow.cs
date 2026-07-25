using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputeOffsetWindow : ExecutionNode
{
    public ExecutionComputeOffsetWindow(
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression? partitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionExpression value,
        ExecutionExpression offset,
        ExecutionExpression defaultValue,
        ExecutionOffsetWindowFunction function,
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeyArray = null,
        ExecutionWindowKeyArray? orderKeyArray = null,
        ExecutionWindowPartitionSet? partitions = null,
        ExecutionWindowPartitionSet? sortedPartitions = null)
    {
        Buffer = buffer;
        Item = item;
        RowAccessMode = rowAccessMode;
        PartitionKey = partitionKey;
        OrderKeys = ExecutionIrCollections.Freeze(orderKeys);
        Value = value;
        Offset = offset;
        DefaultValue = defaultValue;
        Function = function;
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
    public ExecutionExpression Value { get; init; }
    public ExecutionExpression Offset { get; init; }
    public ExecutionExpression DefaultValue { get; init; }
    public ExecutionOffsetWindowFunction Function { get; init; }
    public ExecutionVariable Results { get; init; }
    public ExecutionWindowKeyArray? PartitionKeyArray { get; init; }
    public ExecutionWindowKeyArray? OrderKeyArray { get; init; }
    public ExecutionWindowPartitionSet? Partitions { get; init; }
    public ExecutionWindowPartitionSet? SortedPartitions { get; init; }
}
