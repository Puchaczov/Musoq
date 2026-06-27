namespace Musoq.Evaluator.IR.Expressions;

public sealed record UnaryOp(UnaryOpKind Kind, IrExpression Operand, Type ReturnType)
    : IrExpression(ReturnType);
