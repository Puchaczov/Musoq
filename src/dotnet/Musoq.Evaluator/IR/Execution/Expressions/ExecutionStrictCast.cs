using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStrictCast(
    ExecutionExpression Expression,
    string TargetTypeName,
    ExecutionTypeRef ReturnType,
    ExecutionVariable? Target = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionStrictCast(
        ExecutionExpression expression,
        string targetTypeName,
        Type returnType,
        ExecutionVariable? target = null)
        : this(expression, targetTypeName, ExecutionClrBindingFactory.FromClr(returnType), target)
    {
    }
}
