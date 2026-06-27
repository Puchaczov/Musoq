namespace Musoq.Evaluator.IR.Expressions;

public sealed record Literal(object? Value, Type ReturnType) : IrExpression(ReturnType);
