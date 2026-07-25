using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionComputePluginWindow : ExecutionNode
{
    public ExecutionComputePluginWindow(
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
        IReadOnlyList<ExecutionVariable>? MethodTargets = null)
    {
        this.Buffer = Buffer;
        this.Item = Item;
        this.RowAccessMode = RowAccessMode;
        this.PartitionKey = PartitionKey;
        this.OrderKeys = ExecutionIrCollections.Freeze(OrderKeys);
        this.Value = Value;
        this.Arguments = ExecutionIrCollections.Freeze(Arguments);
        this.RowScopedArguments = ExecutionIrCollections.Freeze(RowScopedArguments);
        this.Frame = Frame;
        this.FactoryMethod = FactoryMethod;
        this.FunctionName = FunctionName;
        this.Results = Results;
        this.PartitionKeyArray = PartitionKeyArray;
        this.OrderKeyArray = OrderKeyArray;
        this.Partitions = Partitions;
        this.SortedPartitions = SortedPartitions;
        this.MethodTargets = MethodTargets == null ? null : ExecutionIrCollections.Freeze(MethodTargets);
    }

    public ExecutionVariable Buffer { get; init; }
    public ExecutionVariable Item { get; init; }
    public ExecutionRowAccessMode RowAccessMode { get; init; }
    public ExecutionExpression? PartitionKey { get; init; }
    public IReadOnlyList<ExecutionWindowOrderKey> OrderKeys { get; init; }
    public ExecutionExpression Value { get; init; }
    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }
    public IReadOnlyList<bool> RowScopedArguments { get; init; }
    public ExecutionWindowFrame? Frame { get; init; }
    public ExecutionCallableRef FactoryMethod { get; init; }
    public string FunctionName { get; init; }
    public ExecutionVariable Results { get; init; }
    public ExecutionWindowKeyArray? PartitionKeyArray { get; init; }
    public ExecutionWindowKeyArray? OrderKeyArray { get; init; }
    public ExecutionWindowPartitionSet? Partitions { get; init; }
    public ExecutionWindowPartitionSet? SortedPartitions { get; init; }
    public IReadOnlyList<ExecutionVariable>? MethodTargets { get; init; }

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
            ExecutionClrBindingFactory.FromClr(factoryMethod),
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
