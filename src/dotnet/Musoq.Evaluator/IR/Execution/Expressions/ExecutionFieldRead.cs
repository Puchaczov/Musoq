using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionFieldRead(
    string? Alias,
    string FieldName,
    ExecutionTypeRef ReturnType,
    FieldAccessStrategy? AccessStrategy = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionFieldRead(
        string? alias,
        string fieldName,
        Type returnType,
        FieldAccessStrategy? accessStrategy = null)
        : this(alias, fieldName, ExecutionClrBindingFactory.FromClr(returnType), accessStrategy)
    {
    }
}
