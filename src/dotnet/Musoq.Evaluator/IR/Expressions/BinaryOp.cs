namespace Musoq.Evaluator.IR.Expressions;

public sealed record BinaryOp(BinaryOpKind Kind, IrExpression Left, IrExpression Right, Type ReturnType)
    : IrExpression(ReturnType);
