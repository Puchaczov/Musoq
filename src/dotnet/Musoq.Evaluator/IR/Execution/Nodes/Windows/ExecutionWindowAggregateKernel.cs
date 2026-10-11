using System.Collections.Generic;

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
    /// <summary>Gets the planner-selected integer ordering strategy.</summary>
    public bool UseIntegerOrderIndex { get; init; }

    public ExecutionVariable Item { get; init; }

    public ExecutionRowAccessMode RowAccessMode { get; init; }

    public ExecutionExpression? PartitionKey { get; init; }

    public IReadOnlyList<ExecutionWindowOrderKey> OrderKeys { get; init; }

    public ExecutionExpression Value { get; init; }

    private IReadOnlyList<ExecutionExpression> _libraryArguments = [];

    public IReadOnlyList<ExecutionExpression> LibraryArguments
    {
        get => _libraryArguments;
        init => _libraryArguments = ExecutionIrCollections.Freeze(value);
    }

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
