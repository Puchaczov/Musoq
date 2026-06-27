using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression CreateMethodCall(
        MethodInfo method,
        IReadOnlyList<ExecutionExpression> arguments,
        string? alias,
        Type returnType,
        ExecutionExpression? injectedSource)
    {
        return new ExecutionMethodCall(
            method,
            arguments,
            alias,
            returnType,
            injectedSource);
    }
}
