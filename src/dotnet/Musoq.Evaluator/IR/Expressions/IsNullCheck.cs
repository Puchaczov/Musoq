namespace Musoq.Evaluator.IR.Expressions;

public sealed record IsNullCheck(IrExpression Expression, bool IsNegated, Type ReturnType)
    : IrExpression(ReturnType);
