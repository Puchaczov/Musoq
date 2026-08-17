using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBinary(
    BinaryOpKind Kind,
    ExecutionExpression Left,
    ExecutionExpression Right,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionBinary(
        BinaryOpKind kind,
        ExecutionExpression left,
        ExecutionExpression right,
        Type returnType)
        : this(kind, left, right, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
