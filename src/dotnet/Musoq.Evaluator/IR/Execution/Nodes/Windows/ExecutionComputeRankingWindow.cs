using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputeRankingWindow(
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    ExecutionRankingWindowFunction Function,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray = null,
    ExecutionWindowKeyArray? OrderKeyArray = null,
    ExecutionWindowPartitionSet? Partitions = null,
    ExecutionWindowPartitionSet? SortedPartitions = null,
    long? QualifyUpperBound = null) : ExecutionNode;
