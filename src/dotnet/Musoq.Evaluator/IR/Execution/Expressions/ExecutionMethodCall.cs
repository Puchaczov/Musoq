using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodCall(
    MethodInfo Method,
    IReadOnlyList<ExecutionExpression> Arguments,
    string? Alias,
    Type ReturnType,
    ExecutionExpression? InjectedSource,
    ExecutionVariable? Target = null,
    ExecutionVariable? Cache = null) : ExecutionExpression(ReturnType)
{
    public ExecutionMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType)
        : this(method, arguments, alias, returnType, null)
    {
    }
}
