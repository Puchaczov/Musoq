namespace Musoq.Evaluator.IR.Expressions;

public sealed record Between(
    IrExpression Expression,
    IrExpression Low,
    IrExpression High,
    Type ReturnType) : IrExpression(ReturnType);
