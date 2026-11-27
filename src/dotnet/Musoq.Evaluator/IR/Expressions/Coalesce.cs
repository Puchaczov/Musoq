namespace Musoq.Evaluator.IR.Expressions;

public sealed record Coalesce(IrExpression[] Expressions, Type ReturnType) : IrExpression(ReturnType)
{
    internal string? InterpretationTypeName { get; init; }
}
