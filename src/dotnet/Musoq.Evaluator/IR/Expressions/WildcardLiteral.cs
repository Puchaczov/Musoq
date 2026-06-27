namespace Musoq.Evaluator.IR.Expressions;

public sealed record WildcardLiteral(Type ReturnType) : IrExpression(ReturnType);
