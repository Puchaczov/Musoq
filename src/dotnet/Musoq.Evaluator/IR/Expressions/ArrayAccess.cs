namespace Musoq.Evaluator.IR.Expressions;

/// <summary>
///     Represents array/string/dictionary indexed access in IR, e.g., array[index] or str[0].
///     Rendered as SafeArrayAccess.GetIndexedElement(array, index, elementType).
/// </summary>
public sealed record ArrayAccess(
    IrExpression Array,
    IrExpression Index,
    Type ElementType,
    Type ReturnType) : IrExpression(ReturnType);
