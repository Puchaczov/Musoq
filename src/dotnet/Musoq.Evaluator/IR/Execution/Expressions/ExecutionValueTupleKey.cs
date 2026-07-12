using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionValueTupleKey(
    IReadOnlyList<ExecutionExpression> Parts,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionValueTupleKey(IReadOnlyList<ExecutionExpression> parts, Type returnType)
        : this(parts, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
