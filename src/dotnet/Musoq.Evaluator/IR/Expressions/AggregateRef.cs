namespace Musoq.Evaluator.IR.Expressions;

public sealed record AggregateRef(string Identifier, Type ReturnType) : IrExpression(ReturnType);
