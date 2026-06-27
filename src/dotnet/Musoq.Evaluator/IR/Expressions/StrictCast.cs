namespace Musoq.Evaluator.IR.Expressions;

public sealed record StrictCast(
    IrExpression Expression,
    string TargetTypeName,
    Type ReturnType) : IrExpression(ReturnType);
