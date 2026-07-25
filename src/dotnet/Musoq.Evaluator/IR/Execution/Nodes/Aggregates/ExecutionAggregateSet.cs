using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateSet : ExecutionNode
{
    public ExecutionAggregateSet(
        ExecutionVariable group,
        ExecutionCallableRef method,
        IReadOnlyList<ExecutionExpression> arguments,
        ExecutionExpression? filterPredicate,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
    {
        Group = group;
        Method = method;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        FilterPredicate = filterPredicate;
        Accumulator = accumulator;
        AccumulatorInput = accumulatorInput;
    }

    public ExecutionVariable Group { get; init; }

    public ExecutionCallableRef Method { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments { get; init; }

    public ExecutionExpression? FilterPredicate { get; init; }

    public AggregateAccumulatorField Accumulator { get; init; }

    public ExecutionExpression? AccumulatorInput { get; init; }

    internal ExecutionAggregateSet(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
        : this(group, ExecutionClrBindingFactory.FromClr(method), arguments, null, accumulator, accumulatorInput)
    {
    }

    internal ExecutionAggregateSet(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        ExecutionExpression? filterPredicate,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
        : this(group, ExecutionClrBindingFactory.FromClr(method), arguments, filterPredicate, accumulator, accumulatorInput)
    {
    }
}
