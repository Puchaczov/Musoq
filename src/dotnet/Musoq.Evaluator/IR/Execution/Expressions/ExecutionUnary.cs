using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionUnary(
    UnaryOpKind Kind,
    ExecutionExpression Operand,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionUnary(UnaryOpKind kind, ExecutionExpression operand, Type returnType)
        : this(kind, operand, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
