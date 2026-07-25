using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAggregateCall : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _arguments = [];

    public ExecutionAggregateCall(
        ExecutionVariable group,
        ExecutionCallableRef method,
        IReadOnlyList<ExecutionExpression> arguments,
        ExecutionTypeRef returnType,
        AggregateAccumulatorField accumulator,
        string? displayName = null)
        : base(returnType)
    {
        Group = group;
        Method = method;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        ReturnType = returnType;
        Accumulator = accumulator;
        DisplayName = displayName;
    }

    public ExecutionVariable Group { get; init; }

    public ExecutionCallableRef Method { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments
    {
        get => _arguments;
        init => _arguments = ExecutionIrCollections.Freeze(value);
    }

    public override ExecutionTypeRef ReturnType { get; init; }

    public AggregateAccumulatorField Accumulator { get; init; }

    public string? DisplayName { get; init; }

    internal ExecutionAggregateCall(
        ExecutionVariable group,
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        Type returnType,
        AggregateAccumulatorField accumulator,
        string? displayName = null)
        : this(group, ExecutionClrBindingFactory.FromClr(method), arguments, ExecutionClrBindingFactory.FromClr(returnType), accumulator, displayName)
    {
    }
}
