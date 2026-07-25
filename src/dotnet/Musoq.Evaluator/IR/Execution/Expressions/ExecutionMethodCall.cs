using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodCall : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _arguments = [];

    public ExecutionMethodCall(
        ExecutionCallableRef method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        ExecutionTypeRef returnType,
        ExecutionExpression? injectedSource,
        ExecutionVariable? target = null,
        ExecutionVariable? cache = null)
        : base(returnType)
    {
        Method = method;
        Arguments = ExecutionIrCollections.Freeze(arguments);
        Alias = alias;
        ReturnType = returnType;
        InjectedSource = injectedSource;
        Target = target;
        Cache = cache;
    }

    public ExecutionCallableRef Method { get; init; }

    public IReadOnlyList<ExecutionExpression> Arguments
    {
        get => _arguments;
        init => _arguments = ExecutionIrCollections.Freeze(value);
    }

    public string? Alias { get; init; }

    public override ExecutionTypeRef ReturnType { get; init; }

    public ExecutionExpression? InjectedSource { get; init; }

    public ExecutionVariable? Target { get; init; }

    public ExecutionVariable? Cache { get; init; }

    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource,
        ExecutionVariable? target = null,
        ExecutionVariable? cache = null)
        : this(ExecutionClrBindingFactory.FromClr(method), arguments, alias, ExecutionClrBindingFactory.FromClr(returnType), injectedSource, target, cache)
    {
    }

    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType)
        : this(ExecutionClrBindingFactory.FromClr(method), arguments, alias, ExecutionClrBindingFactory.FromClr(returnType), null)
    {
    }
}
