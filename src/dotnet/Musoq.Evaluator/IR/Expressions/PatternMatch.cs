namespace Musoq.Evaluator.IR.Expressions;

public sealed record PatternMatch(
    IrExpression Expression,
    IrExpression Pattern,
    PatternKind Kind,
    Type ReturnType) : IrExpression(ReturnType);
