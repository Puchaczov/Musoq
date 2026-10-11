using System.Collections.Generic;

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
    /// <summary>Gets the planner-selected integer ordering strategy.</summary>
    public bool UseIntegerOrderIndex { get; init; }
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
    /// <summary>Gets the validated library operation for concrete partition and peer context.</summary>
    public ExecutionCallableRef? LibraryGet { get; init; }
    /// <summary>Gets whether the registered operation requires peer bounds and dense rank.</summary>
    public bool RequiresPeerMetadata { get; init; } = true;
}
