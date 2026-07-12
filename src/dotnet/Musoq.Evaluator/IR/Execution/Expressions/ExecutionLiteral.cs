using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLiteral(
    ExecutionConstantValue Value,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionLiteral(object? value, Type returnType)
        : this(ExecutionConstantValue.FromClr(value, ExecutionTypeRef.FromClr(returnType)), ExecutionTypeRef.FromClr(returnType))
    {
    }

    internal ExecutionLiteral(object? value, ExecutionTypeRef returnType)
        : this(ExecutionConstantValue.FromClr(value, returnType), returnType)
    {
    }
}
