using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateSet(
    ExecutionVariable Group,
    MethodInfo Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    ExecutionExpression? FilterPredicate,
    AggregateAccumulatorField Accumulator,
    ExecutionExpression? AccumulatorInput) : ExecutionNode
{
    public ExecutionAggregateSet(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        AggregateAccumulatorField accumulator,
        ExecutionExpression? accumulatorInput)
        : this(group, method, arguments, null, accumulator, accumulatorInput)
    {
    }
}
