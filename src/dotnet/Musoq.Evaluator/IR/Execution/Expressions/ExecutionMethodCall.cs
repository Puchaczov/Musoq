using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodCall(
    ExecutionCallableRef Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    string? Alias,
    ExecutionTypeRef ReturnType,
    ExecutionExpression? InjectedSource,
    ExecutionVariable? Target = null,
    ExecutionVariable? Cache = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource,
        ExecutionVariable? target = null,
        ExecutionVariable? cache = null)
        : this(ExecutionCallableRef.FromClr(method), arguments, alias, ExecutionTypeRef.FromClr(returnType), injectedSource, target, cache)
    {
    }

    internal ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType)
        : this(ExecutionCallableRef.FromClr(method), arguments, alias, ExecutionTypeRef.FromClr(returnType), null)
    {
    }
}
