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
    ExecutionCallableRef FactoryMethod,
    string FunctionName,
    ExecutionVariable Results,
    ExecutionWindowKeyArray? PartitionKeyArray = null,
    ExecutionWindowKeyArray? OrderKeyArray = null,
    ExecutionWindowPartitionSet? Partitions = null,
    ExecutionWindowPartitionSet? SortedPartitions = null,
    IReadOnlyList<ExecutionVariable>? MethodTargets = null) : ExecutionNode
{
    internal ExecutionComputePluginWindow(
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression? partitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionExpression value,
        IReadOnlyList<ExecutionExpression> arguments,
        IReadOnlyList<bool> rowScopedArguments,
        ExecutionWindowFrame? frame,
        MethodInfo factoryMethod,
        string functionName,
        ExecutionVariable results,
        ExecutionWindowKeyArray? partitionKeyArray = null,
        ExecutionWindowKeyArray? orderKeyArray = null,
        ExecutionWindowPartitionSet? partitions = null,
        ExecutionWindowPartitionSet? sortedPartitions = null,
        IReadOnlyList<ExecutionVariable>? methodTargets = null)
        : this(
            buffer,
            item,
            rowAccessMode,
            partitionKey,
            orderKeys,
            value,
            arguments,
            rowScopedArguments,
            frame,
            ExecutionCallableRef.FromClr(factoryMethod),
            functionName,
            results,
            partitionKeyArray,
            orderKeyArray,
            partitions,
            sortedPartitions,
            methodTargets)
    {
    }
}
