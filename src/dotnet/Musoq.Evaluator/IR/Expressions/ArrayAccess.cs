namespace Musoq.Evaluator.IR.Expressions;

/// <summary>
///     Represents array/string/dictionary indexed access in IR, e.g., array[index] or str[0].
///     Lowered to a concrete indexed operation before rendering.
/// </summary>
public sealed record ArrayAccess(
    IrExpression Array,
    IrExpression Index,
    Type ElementType,
    Type ReturnType) : IrExpression(ReturnType);
