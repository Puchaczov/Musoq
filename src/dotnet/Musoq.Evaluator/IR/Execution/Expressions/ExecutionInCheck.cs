using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInCheck(
    ExecutionExpression Expression,
    IReadOnlyList<ExecutionExpression> Values,
    ExecutionTypeRef ReturnType,
    ExecutionConstantInSet? ConstantSet = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionInCheck(
        ExecutionExpression expression,
        IReadOnlyList<ExecutionExpression> values,
        Type returnType,
        ExecutionConstantInSet? constantSet = null)
        : this(expression, values, ExecutionTypeRef.FromClr(returnType), constantSet)
    {
    }
}
