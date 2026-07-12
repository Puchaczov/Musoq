using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBetween(
    ExecutionExpression Expression,
    ExecutionExpression Low,
    ExecutionExpression High,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionBetween(
        ExecutionExpression expression,
        ExecutionExpression low,
        ExecutionExpression high,
        Type returnType)
        : this(expression, low, high, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
