using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateSet(
    ExecutionVariable Group,
    MethodInfo Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    AggregateAccumulatorField Accumulator,
    ExecutionExpression? AccumulatorInput) : ExecutionNode;
