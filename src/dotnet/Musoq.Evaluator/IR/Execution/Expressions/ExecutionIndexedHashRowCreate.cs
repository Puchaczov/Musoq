using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowCreate(
    ExecutionVariable Row,
    ExecutionVariable Index,
    ExecutionTypeRef ReturnType,
    string? GeneratedRowTypeName = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionIndexedHashRowCreate(
        ExecutionVariable row,
        ExecutionVariable index,
        Type returnType,
        string? generatedRowTypeName = null)
        : this(row, index, ExecutionTypeRef.FromClr(returnType), generatedRowTypeName)
    {
    }
}
