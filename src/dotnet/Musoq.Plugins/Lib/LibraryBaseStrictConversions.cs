using System;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Lib.RuntimeOperators;
using Musoq.Plugins.Lib.TypeConversion;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #region Dependency Injection - SOLID Principle: Dependency Inversion

    /// <summary>
    ///     Type converters and runtime operators following SOLID principles.
    ///     These are initialized as static singletons but can be replaced for testing.
    /// </summary>
    private static readonly StrictTypeConverter StrictConverter = new();

    private static readonly ComparisonTypeConverter ComparisonConverter = new();
    private static readonly NumericOnlyTypeConverter NumericOnlyConverter = new();

    private static readonly TypePreservingRuntimeOperators RuntimeOperators = new(
        NumericOnlyConverter,
        ComparisonConverter,
        StrictConverter);

    #endregion

    #region Strict Conversions (Reject Precision Loss)

    /// <summary>
    ///     Attempts to convert a value to Int32 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Int32
    ///     - Values outside the Int32 range (int.MinValue to int.MaxValue)
    ///     - Strings that cannot be parsed as valid Int32 values
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Strict(object? value)
    {
        return StrictConverter.TryConvertToInt32(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int64 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Int64
    ///     - Values outside the Int64 range (long.MinValue to long.MaxValue)
    ///     - Strings that cannot be parsed as valid Int64 values
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Strict(object? value)
    {
        return StrictConverter.TryConvertToInt64(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method rejects conversions that would result in precision loss, such as:
    ///     - Floating-point values that cannot be exactly represented as Decimal
    ///     - NaN or Infinity values
    ///     - Strings that cannot be parsed as valid Decimal values
    ///     Decimal has a larger range and precision than Int32/Int64 for fractional values.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalStrict(object? value)
    {
        return StrictConverter.TryConvertToDecimal(value);
    }

    #endregion

    #region Comparison-Mode Conversions (Allow Lossy Conversions)

    /// <summary>
    ///     Attempts to convert a value to Int32 for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int32 range.
    ///     Useful for comparison operations where approximate values are acceptable, such as:
    ///     - Comparing floating-point values to integers (e.g., 3.0 == 3)
    ///     - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt32(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Int64 for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int64 range.
    ///     Useful for comparison operations where approximate values are acceptable, such as:
    ///     - Comparing floating-point values to long integers
    ///     - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt64(value);
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal for comparison operations, allowing precision loss but validating range
    ///     constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if within valid range; otherwise, null.</returns>
    /// <remarks>
    ///     This method allows lossy conversions but rejects NaN and Infinity values.
    ///     Decimal has a very large range, so most numeric values can be converted successfully.
    ///     Useful for comparison operations where high precision is needed but some loss is acceptable.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalComparison(object? value)
    {
        return ComparisonConverter.TryConvertToDecimal(value);
    }

    #endregion

    #region Numeric-Only Conversions (Reject Strings)

    /// <summary>
    ///     Smart numeric conversion that automatically selects the appropriate target type (Int32, Int64, or Decimal)
    ///     based on the actual value. Tries Int32 first, then Int64, then Decimal, returning the first successful conversion
    ///     as Decimal.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted value as Decimal; null if conversion fails.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It automatically handles integers, longs, floats, doubles, and decimals, including fractional values.
    ///     Returns Decimal to support all numeric types and enable compile-time operator usage.
    ///     Rejects strings, booleans, and other non-numeric types.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertNumericOnly(object? value)
    {
        if (value == null)
            return null;

        var int32Result = TryConvertToInt32NumericOnly(value);
        if (int32Result.HasValue)
            return int32Result.Value;

        var int64Result = TryConvertToInt64NumericOnly(value);
        if (int64Result.HasValue)
            return int64Result.Value;

        var decimalResult = TryConvertToDecimalNumericOnly(value);
        return decimalResult;
    }

    private T? TryConvertToIntegralTypeNumericOnly<T>(object? value, Func<object, T> converter,
        Func<T, T, bool> precisionCheck) where T : struct
    {
        if (value == null)
            return null;

        try
        {
            switch (value)
            {
                case T directValue:
                    return directValue;

                case byte byteValue:
                    return converter(byteValue);

                case sbyte sbyteValue:
                    return converter(sbyteValue);

                case short shortValue:
                    return converter(shortValue);

                case ushort ushortValue:
                    return converter(ushortValue);

                case int intValue:
                    return converter(intValue);

                case uint uintValue:
                    var uintResult = converter(uintValue);
                    return precisionCheck(uintResult, default) ? uintResult : null;

                case long longValue:
                    var longResult = converter(longValue);
                    return precisionCheck(longResult, default) ? longResult : null;

                case ulong ulongValue:
                    var ulongResult = converter(ulongValue);
                    return precisionCheck(ulongResult, default) ? ulongResult : null;

                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;

                    var resultFromFloat = converter(floatValue);
                    if (!precisionCheck(resultFromFloat, default))
                        return null;

                    var floatBack = Convert.ToSingle(resultFromFloat);
                    if (Math.Abs(floatValue - floatBack) > float.Epsilon)
                        return null;
                    return resultFromFloat;

                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;

                    var resultFromDouble = converter(doubleValue);
                    if (!precisionCheck(resultFromDouble, default))
                        return null;

                    var doubleBack = Convert.ToDouble(resultFromDouble);
                    if (Math.Abs(doubleValue - doubleBack) > double.Epsilon)
                        return null;
                    return resultFromDouble;

                case decimal decimalValue:
                    var resultFromDecimal = converter(decimalValue);
                    if (!precisionCheck(resultFromDecimal, default))
                        return null;

                    var decimalBack = Convert.ToDecimal(resultFromDecimal);
                    if (decimalValue != decimalBack)
                        return null;
                    return resultFromDecimal;

                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Attempts to convert a value to Int32, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32NumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly(
            value,
            obj =>
            {
                try
                {
                    return Convert.ToInt32(obj);
                }
                catch
                {
                    return 0;
                }
            },
            (_, _) =>
            {
                return value switch
                {
                    uint and > int.MaxValue or long and (< int.MinValue or > int.MaxValue)
                        or ulong and > int.MaxValue => false,
                    _ => true
                };
            });
    }

    /// <summary>
    ///     Attempts to convert a value to Int64, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64NumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly(
            value,
            obj =>
            {
                try
                {
                    return Convert.ToInt64(obj);
                }
                catch
                {
                    return 0L;
                }
            },
            (_, _) =>
            {
                if (value is ulong and > long.MaxValue)
                    return false;
                return true;
            });
    }

    /// <summary>
    ///     Attempts to convert a value to Decimal, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    ///     This method is used for arithmetic operations on System.Object columns.
    ///     It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalNumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly(
            value,
            obj =>
            {
                try
                {
                    if (obj is double doubleValue && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)))
                        throw new InvalidOperationException();
                    return Convert.ToDecimal(obj);
                }
                catch
                {
                    return 0m;
                }
            },
            (_, _) => true);
    }

    #endregion

    #region Runtime Operator Methods

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
        if (value == null)
            return null;

        if (value is string)
            return null;

        try
        {
            switch (value)
            {
                case double d:
                    return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
                case float f:
                    return float.IsNaN(f) || float.IsInfinity(f) ? null : (double)f;
                default:
                    return Convert.ToDouble(value);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Runtime greater than operator that handles object &gt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThan(left, right);
    }

    /// <summary>
    ///     Runtime less than operator that handles object &lt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThan(left, right);
    }

    /// <summary>
    ///     Runtime greater than or equal operator that handles object &gt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThanOrEqual(left, right);
    }

    /// <summary>
    ///     Runtime less than or equal operator that handles object &lt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThanOrEqual(left, right);
    }

    /// <summary>
    ///     Runtime equality operator that handles object == object with automatic type conversion.
    ///     For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    ///     Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.Equal(left, right);
    }

    /// <summary>
    ///     Runtime inequality operator that handles object != object with automatic type conversion.
    ///     For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    ///     Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalNotEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.NotEqual(left, right);
    }

    #endregion
}
