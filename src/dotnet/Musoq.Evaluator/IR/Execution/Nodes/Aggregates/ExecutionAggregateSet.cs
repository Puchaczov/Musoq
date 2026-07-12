using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateSet(
    ExecutionVariable Group,
    ExecutionCallableRef Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    ExecutionExpression? FilterPredicate,
    AggregateAccumulatorField Accumulator,
    ExecutionExpression? AccumulatorInput) : ExecutionNode
{
    internal ExecutionAggregateSet(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
        : this(group, ExecutionCallableRef.FromClr(method), arguments, null, accumulator, accumulatorInput)
    {
    }

    internal ExecutionAggregateSet(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        ExecutionExpression? filterPredicate,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
        : this(group, ExecutionCallableRef.FromClr(method), arguments, filterPredicate, accumulator, accumulatorInput)
    {
    }
}
