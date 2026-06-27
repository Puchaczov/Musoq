namespace Musoq.Evaluator.IR.Expressions;

public sealed record ColumnRef(string Alias, string ColumnName, Type ReturnType) : IrExpression(ReturnType);
