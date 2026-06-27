using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Runtime addition operator that handles object + object with type-preserving conversion.
    ///     Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    ///     Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>Sum of the operands in the appropriate numeric type, or null if conversion fails or operands are invalid.</returns>
    [BindableMethod(true)]
    public object? InternalApplyAddOperator(object? left, object? right)
    {
        return RuntimeOperators.Add(left, right);
    }

    /// <summary>
    ///     Runtime subtraction operator that handles object - object with type-preserving conversion.
    ///     Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    ///     Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>
    ///     Difference of the operands in the appropriate numeric type, or null if conversion fails or operands are
    ///     invalid.
    /// </returns>
    [BindableMethod(true)]
    public object? InternalApplySubtractOperator(object? left, object? right)
    {
        return RuntimeOperators.Subtract(left, right);
    }

    /// <summary>
    ///     Runtime multiplication operator that handles object * object with type-preserving conversion.
    ///     Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    ///     Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>Product of the operands in the appropriate numeric type, or null if conversion fails or operands are invalid.</returns>
    [BindableMethod(true)]
    public object? InternalApplyMultiplyOperator(object? left, object? right)
    {
        return RuntimeOperators.Multiply(left, right);
    }

    /// <summary>
    ///     Runtime division operator that handles object / object with type-preserving conversion.
    ///     Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    ///     Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>Quotient of the operands in the appropriate numeric type, or null if conversion fails or operands are invalid.</returns>
    [BindableMethod(true)]
    public object? InternalApplyDivideOperator(object? left, object? right)
    {
        return RuntimeOperators.Divide(left, right);
    }

    /// <summary>
    ///     Runtime modulo operator that handles object % object with type-preserving conversion.
    ///     Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    ///     Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>
    ///     Remainder of the operands in the appropriate numeric type, or null if conversion fails or operands are
    ///     invalid.
    /// </returns>
    [BindableMethod(true)]
    public object? InternalApplyModuloOperator(object? left, object? right)
    {
        return RuntimeOperators.Modulo(left, right);
    }

    /// <summary>
    ///     Attempts to convert a value to Double, rejecting strings and accepting only boxed numeric types.
    ///     Rejects NaN and Infinity values for safety.
    /// </summary>
    /// <param name="value">The value to convert (must be boxed numeric type, not string).</param>
    /// <returns>Converted double value, or null if conversion fails or value is invalid.</returns>
    [BindableMethod(true)]
    public double? TryConvertToDoubleNumericOnly(object? value)
    {
        return NumericOnlyConverter.TryConvertToDouble(value);
    }
}
