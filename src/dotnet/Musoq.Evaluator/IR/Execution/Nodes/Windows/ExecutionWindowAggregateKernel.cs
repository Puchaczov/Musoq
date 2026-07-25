using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowAggregateKernel : ExecutionNode
{
    public ExecutionWindowAggregateKernel(
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression? partitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionExpression value,
        ExecutionExpression? filterPredicate,
        ExecutionWindowFrame? frame,
        ExecutionWindowAggregateKernelDescriptor descriptor,
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeyArray = null,
        ExecutionWindowKeyArray? orderKeyArray = null,
        ExecutionWindowPartitionSet? partitions = null,
        ExecutionWindowPartitionSet? sortedPartitions = null,
        IReadOnlyList<ExecutionVariable>? methodTargets = null)
    {
        Buffer = buffer;
        Item = item;
        RowAccessMode = rowAccessMode;
        PartitionKey = partitionKey;
        OrderKeys = ExecutionIrCollections.Freeze(orderKeys);
        Value = value;
        FilterPredicate = filterPredicate;
        Frame = frame;
        Descriptor = descriptor;
        Results = results;
        PartitionKeyArray = partitionKeyArray;
        OrderKeyArray = orderKeyArray;
        Partitions = partitions;
        SortedPartitions = sortedPartitions;
        MethodTargets = methodTargets == null ? null : ExecutionIrCollections.Freeze(methodTargets);
    }

    public ExecutionVariable Buffer { get; init; }

    public ExecutionVariable Item { get; init; }

    public ExecutionRowAccessMode RowAccessMode { get; init; }

    public ExecutionExpression? PartitionKey { get; init; }

    public IReadOnlyList<ExecutionWindowOrderKey> OrderKeys { get; init; }

    public ExecutionExpression Value { get; init; }

    public ExecutionExpression? FilterPredicate { get; init; }

    public ExecutionWindowFrame? Frame { get; init; }

    public ExecutionWindowAggregateKernelDescriptor Descriptor { get; init; }

    public ExecutionVariable Results { get; init; }

    public ExecutionWindowKeyArray? PartitionKeyArray { get; init; }

    public ExecutionWindowKeyArray? OrderKeyArray { get; init; }

    public ExecutionWindowPartitionSet? Partitions { get; init; }

    public ExecutionWindowPartitionSet? SortedPartitions { get; init; }

    public IReadOnlyList<ExecutionVariable>? MethodTargets { get; init; }
}
