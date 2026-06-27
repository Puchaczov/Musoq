using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputeOffsetWindow(
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    ExecutionExpression Value,
    ExecutionExpression Offset,
    ExecutionExpression DefaultValue,
    ExecutionOffsetWindowFunction Function,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray = null,
    ExecutionWindowKeyArray? OrderKeyArray = null,
    ExecutionWindowPartitionSet? Partitions = null,
    ExecutionWindowPartitionSet? SortedPartitions = null) : ExecutionNode;
