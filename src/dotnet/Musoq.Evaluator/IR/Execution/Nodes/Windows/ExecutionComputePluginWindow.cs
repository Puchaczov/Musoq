using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputePluginWindow(
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression? PartitionKey,
    IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
    ExecutionExpression Value,
    IReadOnlyList<ExecutionExpression> Arguments,
    IReadOnlyList<bool> RowScopedArguments,
    ExecutionWindowFrame? Frame,
    MethodInfo FactoryMethod,
    string FunctionName,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray = null,
    ExecutionWindowKeyArray? OrderKeyArray = null,
    ExecutionWindowPartitionSet? Partitions = null,
    ExecutionWindowPartitionSet? SortedPartitions = null,
    IReadOnlyList<ExecutionVariable>? MethodTargets = null) : ExecutionNode;
