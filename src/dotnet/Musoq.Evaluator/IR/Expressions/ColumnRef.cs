namespace Musoq.Evaluator.IR.Expressions;

public sealed record ColumnRef(string Alias, string ColumnName, Type ReturnType,
    string? GeneratedTypeName = null) : IrExpression(ReturnType);
