using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal sealed record ExecutionWindowComputationMetadata(
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray,
    ExecutionWindowKeyArray? OrderKeyArray,
    ExecutionWindowPartitionSet? Partitions,
    ExecutionWindowPartitionSet? SortedPartitions);
