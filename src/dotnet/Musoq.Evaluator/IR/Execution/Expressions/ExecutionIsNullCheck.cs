using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIsNullCheck(
    ExecutionExpression Expression,
    bool IsNegated,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionIsNullCheck(ExecutionExpression expression, bool isNegated, Type returnType)
        : this(expression, isNegated, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
