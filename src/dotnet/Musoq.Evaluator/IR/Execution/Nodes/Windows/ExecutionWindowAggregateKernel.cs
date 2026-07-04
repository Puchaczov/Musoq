using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowAggregateKernel(
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    ExecutionExpression Value,
    ExecutionExpression? FilterPredicate,
    ExecutionWindowFrame? Frame,
    ExecutionWindowAggregateKernelDescriptor Descriptor,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray = null,
    ExecutionWindowKeyArray? OrderKeyArray = null,
    ExecutionWindowPartitionSet? Partitions = null,
    ExecutionWindowPartitionSet? SortedPartitions = null,
    IReadOnlyList<ExecutionVariable>? MethodTargets = null) : ExecutionNode;
