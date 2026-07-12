using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCoalesce(
    IReadOnlyList<ExecutionExpression> Expressions,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionCoalesce(IReadOnlyList<ExecutionExpression> expressions, Type returnType)
        : this(expressions, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
