using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCall(
    ExecutionVariable Group,
    MethodInfo Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    Type ReturnType,
    AggregateAccumulatorField Accumulator,
    string? DisplayName = null) : ExecutionExpression(ReturnType);
