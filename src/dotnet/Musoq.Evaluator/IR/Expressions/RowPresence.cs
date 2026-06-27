namespace Musoq.Evaluator.IR.Expressions;

public sealed record RowPresence(string Alias, bool IsPresent, Type ReturnType) : IrExpression(ReturnType);
