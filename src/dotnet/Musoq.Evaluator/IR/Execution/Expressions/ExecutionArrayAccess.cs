using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionArrayAccess(
    ExecutionExpression Array,
    ExecutionExpression Index,
    ExecutionTypeRef ElementType,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionArrayAccess(
        ExecutionExpression array,
        ExecutionExpression index,
        Type elementType,
        Type returnType)
        : this(array, index, ExecutionTypeRef.FromClr(elementType), ExecutionTypeRef.FromClr(returnType))
    {
    }
}
