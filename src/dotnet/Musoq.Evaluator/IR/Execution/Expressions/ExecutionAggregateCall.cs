using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCall(
    ExecutionVariable Group,
    ExecutionCallableRef Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    ExecutionTypeRef ReturnType,
    AggregateAccumulatorField Accumulator,
    string? DisplayName = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionAggregateCall(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        Type returnType,
        AggregateAccumulatorField accumulator,
        string? displayName = null)
        : this(group, ExecutionCallableRef.FromClr(method), arguments, ExecutionTypeRef.FromClr(returnType), accumulator, displayName)
    {
    }
}
