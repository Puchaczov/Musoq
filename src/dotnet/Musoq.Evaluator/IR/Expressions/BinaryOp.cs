namespace Musoq.Evaluator.IR.Expressions;

public sealed record BinaryOp(BinaryOpKind Kind, IrExpression Left, IrExpression Right, Type ReturnType)
    : IrExpression(ReturnType)
{
    /// <summary>Indicates SQL three-valued comparison semantics; synthetic comparisons leave it disabled.</summary>
    public bool UsesSqlNullSemantics { get; init; }
}
