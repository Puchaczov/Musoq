using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBinary(
    BinaryOpKind Kind,
    ExecutionExpression Left,
    ExecutionExpression Right,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    /// <summary>
    ///     Indicates that this comparison originated from SQL comparison syntax and therefore uses
    ///     three-valued NULL semantics. Synthetic comparisons created by planning and lowering leave
    ///     this disabled so their existing CLR semantics are preserved.
    /// </summary>
    public bool UsesSqlNullSemantics { get; init; }

    internal ExecutionBinary(
        BinaryOpKind kind,
        ExecutionExpression left,
        ExecutionExpression right,
        Type returnType)
        : this(kind, left, right, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
