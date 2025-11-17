using System;
using System.Globalization;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Lib.TypeConversion;
using Musoq.Plugins.Lib.RuntimeOperators;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    #region Dependency Injection - SOLID Principle: Dependency Inversion

    /// <summary>
    /// Type converters and runtime operators following SOLID principles.
    /// These are initialized as static singletons but can be replaced for testing.
    /// </summary>
    private static readonly ITypeConverter StrictConverter = new StrictTypeConverter();
    private static readonly ITypeConverter ComparisonConverter = new ComparisonTypeConverter();
    private static readonly ITypeConverter NumericOnlyConverter = new NumericOnlyTypeConverter();
    private static readonly IRuntimeOperators RuntimeOperators = new TypePreservingRuntimeOperators(
        NumericOnlyConverter,
        ComparisonConverter,
        StrictConverter);

    #endregion

    #region Strict Conversions (Reject Precision Loss)

    /// <summary>
    /// Generic helper method for strict type conversions that reject any precision loss.
    /// </summary>
    /// <typeparam name="T">The target numeric type (int, long, or decimal).</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="converter">Function to perform the conversion to type T.</param>
    /// <param name="precisionCheck">Function to validate that the conversion preserves precision.</param>
    /// <returns>The converted value if successful and no precision is lost; otherwise, null.</returns>
    private T? TryConvertToIntegralTypeStrict<T>(object? value, Func<object, T> converter, Func<T, T, bool> precisionCheck) where T : struct
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
                    return precisionCheck(uintResult, default) ? uintResult : (T?)null;
                
                case long longValue:
                    var longResult = converter(longValue);
                    return precisionCheck(longResult, default) ? longResult : (T?)null;
                
                case ulong ulongValue:
                    var ulongResult = converter(ulongValue);
                    return precisionCheck(ulongResult, default) ? ulongResult : (T?)null;
                
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
                
                case string stringValue:
                    if (typeof(T) == typeof(int) && int.TryParse(stringValue, out var parsedInt))
                        return (T)(object)parsedInt;
                    if (typeof(T) == typeof(long) && long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                        return (T)(object)parsedLong;
                    if (typeof(T) == typeof(decimal) && decimal.TryParse(stringValue, out var parsedDecimal))
                        return (T)(object)parsedDecimal;
                    return null;
                
                case bool boolValue:
                    if (typeof(T) == typeof(int))
                        return (T)(object)(boolValue ? 1 : 0);
                    if (typeof(T) == typeof(long))
                        return (T)(object)(boolValue ? 1L : 0L);
                    if (typeof(T) == typeof(decimal))
                        return (T)(object)(boolValue ? 1m : 0m);
                    return null;
                
                default:
                    return converter(value);
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to convert a value to Int32 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method rejects conversions that would result in precision loss, such as:
    /// - Floating-point values that cannot be exactly represented as Int32
    /// - Values outside the Int32 range (int.MinValue to int.MaxValue)
    /// - Strings that cannot be parsed as valid Int32 values
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Strict(object? value)
    {
        return StrictConverter.TryConvertToInt32(value);
    }

    /// <summary>
    /// Attempts to convert a value to Int64 with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method rejects conversions that would result in precision loss, such as:
    /// - Floating-point values that cannot be exactly represented as Int64
    /// - Values outside the Int64 range (long.MinValue to long.MaxValue)
    /// - Strings that cannot be parsed as valid Int64 values
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Strict(object? value)
    {
        return StrictConverter.TryConvertToInt64(value);
    }

    /// <summary>
    /// Attempts to convert a value to Decimal with strict validation, rejecting any conversions that would lose precision.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method rejects conversions that would result in precision loss, such as:
    /// - Floating-point values that cannot be exactly represented as Decimal
    /// - NaN or Infinity values
    /// - Strings that cannot be parsed as valid Decimal values
    /// Decimal has a larger range and precision than Int32/Int64 for fractional values.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalStrict(object? value)
    {
        return StrictConverter.TryConvertToDecimal(value);
    }

    #endregion

    #region Comparison-Mode Conversions (Allow Lossy Conversions)

    /// <summary>
    /// Attempts to convert a value to Int32 for comparison operations, allowing precision loss but validating range constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int32 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    /// This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int32 range.
    /// Useful for comparison operations where approximate values are acceptable, such as:
    /// - Comparing floating-point values to integers (e.g., 3.0 == 3)
    /// - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt32(value);
    }

    /// <summary>
    /// Attempts to convert a value to Int64 for comparison operations, allowing precision loss but validating range constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Int64 value if within valid range; otherwise, null.</returns>
    /// <remarks>
    /// This method allows lossy conversions (e.g., 3.7 becomes 3) but rejects values outside the Int64 range.
    /// Useful for comparison operations where approximate values are acceptable, such as:
    /// - Comparing floating-point values to long integers
    /// - Range checks that tolerate fractional truncation
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64Comparison(object? value)
    {
        return ComparisonConverter.TryConvertToInt64(value);
    }

    /// <summary>
    /// Attempts to convert a value to Decimal for comparison operations, allowing precision loss but validating range constraints.
    /// </summary>
    /// <param name="value">The value to convert. Can be any numeric type, string, or boolean.</param>
    /// <returns>The converted Decimal value if within valid range; otherwise, null.</returns>
    /// <remarks>
    /// This method allows lossy conversions but rejects NaN and Infinity values.
    /// Decimal has a very large range, so most numeric values can be converted successfully.
    /// Useful for comparison operations where high precision is needed but some loss is acceptable.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalComparison(object? value)
    {
        return ComparisonConverter.TryConvertToDecimal(value);
    }

    #endregion

    #region Numeric-Only Conversions (Reject Strings)

    /// <summary>
    /// Smart numeric conversion that automatically selects the appropriate target type (Int32, Int64, or Decimal)
    /// based on the actual value. Tries Int32 first, then Int64, then Decimal, returning the first successful conversion as Decimal.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted value as Decimal; null if conversion fails.</returns>
    /// <remarks>
    /// This method is used for arithmetic operations on System.Object columns.
    /// It automatically handles integers, longs, floats, doubles, and decimals, including fractional values.
    /// Returns Decimal to support all numeric types and enable compile-time operator usage.
    /// Rejects strings, booleans, and other non-numeric types.
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertNumericOnly(object? value)
    {
        if (value == null)
            return null;

        // Try Int32 first (most common case) - convert to decimal
        var int32Result = TryConvertToInt32NumericOnly(value);
        if (int32Result.HasValue)
            return (decimal)int32Result.Value;

        // Try Int64 for larger integers - convert to decimal
        var int64Result = TryConvertToInt64NumericOnly(value);
        if (int64Result.HasValue)
            return (decimal)int64Result.Value;

        // Fall back to Decimal for fractional values
        var decimalResult = TryConvertToDecimalNumericOnly(value);
        if (decimalResult.HasValue)
            return decimalResult.Value;

        // All conversions failed
        return null;
    }

    /// <summary>
    /// Generic helper method for numeric-only type conversions that reject strings entirely.
    /// </summary>
    /// <typeparam name="T">The target numeric type (int, long, or decimal).</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="converter">Function to perform the conversion to type T.</param>
    /// <param name="precisionCheck">Function to validate that the conversion preserves precision.</param>
    /// <returns>The converted value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This conversion mode is used for arithmetic operations on System.Object columns.
    /// It only accepts boxed numeric types and rejects strings, booleans, and other non-numeric types.
    /// </remarks>
    private T? TryConvertToIntegralTypeNumericOnly<T>(object? value, Func<object, T> converter, Func<T, T, bool> precisionCheck) where T : struct
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
                    return precisionCheck(uintResult, default) ? uintResult : (T?)null;
                
                case long longValue:
                    var longResult = converter(longValue);
                    return precisionCheck(longResult, default) ? longResult : (T?)null;
                
                case ulong ulongValue:
                    var ulongResult = converter(ulongValue);
                    return precisionCheck(ulongResult, default) ? ulongResult : (T?)null;
                
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
                
                // Reject strings, booleans, and other non-numeric types
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
    /// Attempts to convert a value to Int32, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int32 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method is used for arithmetic operations on System.Object columns.
    /// It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public int? TryConvertToInt32NumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly<int>(
            value,
            obj =>
            {
                try { return Convert.ToInt32(obj); }
                catch { return 0; }
            },
            (result, _) =>
            {
                if (value is uint uintValue && uintValue > int.MaxValue)
                    return false;
                if (value is long longValue && (longValue < int.MinValue || longValue > int.MaxValue))
                    return false;
                if (value is ulong ulongValue && ulongValue > int.MaxValue)
                    return false;
                return true;
            });
    }

    /// <summary>
    /// Attempts to convert a value to Int64, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Int64 value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method is used for arithmetic operations on System.Object columns.
    /// It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public long? TryConvertToInt64NumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly<long>(
            value,
            obj =>
            {
                try { return Convert.ToInt64(obj); }
                catch { return 0L; }
            },
            (result, _) =>
            {
                if (value is ulong ulongValue && ulongValue > long.MaxValue)
                    return false;
                return true;
            });
    }

    /// <summary>
    /// Attempts to convert a value to Decimal, rejecting strings and accepting only boxed numeric types.
    /// </summary>
    /// <param name="value">The value to convert. Must be a boxed numeric type.</param>
    /// <returns>The converted Decimal value if successful and no precision is lost; otherwise, null.</returns>
    /// <remarks>
    /// This method is used for arithmetic operations on System.Object columns.
    /// It rejects string values and only accepts boxed numeric types (int, long, double, etc.).
    /// </remarks>
    [BindableMethod(true)]
    public decimal? TryConvertToDecimalNumericOnly(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly<decimal>(
            value,
            obj =>
            {
                try
                {
                    if (obj is double doubleValue && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)))
                        throw new InvalidOperationException();
                    return Convert.ToDecimal(obj);
                }
                catch { return 0m; }
            },
            (result, _) => true);
    }

    #endregion

    #region Runtime Operator Methods

    /// <summary>
    /// Runtime addition operator that handles object + object with type-preserving conversion.
    /// Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    /// Priority: decimal > double > long.
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
    /// Runtime subtraction operator that handles object - object with type-preserving conversion.
    /// Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    /// Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>Difference of the operands in the appropriate numeric type, or null if conversion fails or operands are invalid.</returns>
    [BindableMethod(true)]
    public object? InternalApplySubtractOperator(object? left, object? right)
    {
        return RuntimeOperators.Subtract(left, right);
    }

    /// <summary>
    /// Runtime multiplication operator that handles object * object with type-preserving conversion.
    /// Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    /// Priority: decimal > double > long.
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
    /// Runtime division operator that handles object / object with type-preserving conversion.
    /// Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    /// Priority: decimal > double > long.
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
    /// Runtime modulo operator that handles object % object with type-preserving conversion.
    /// Automatically selects the appropriate numeric type (long, double, or decimal) based on operand types.
    /// Priority: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <returns>Remainder of the operands in the appropriate numeric type, or null if conversion fails or operands are invalid.</returns>
    [BindableMethod(true)]
    public object? InternalApplyModuloOperator(object? left, object? right)
    {
        return RuntimeOperators.Modulo(left, right);
    }

    /// <summary>
    /// Helper method that applies arithmetic operations with type-preserving conversion rules.
    /// Type selection priority: decimal > double > long (promotes to wider type when mixing types).
    /// Rejects string operands for arithmetic operations.
    /// </summary>
    /// <param name="left">Left operand (boxed numeric type).</param>
    /// <param name="right">Right operand (boxed numeric type).</param>
    /// <param name="longOp">Function to execute when both operands are integer types.</param>
    /// <param name="doubleOp">Function to execute when operands contain float or double types.</param>
    /// <param name="decimalOp">Function to execute when operands contain decimal type.</param>
    /// <returns>Result of the operation in the appropriate numeric type, or null if conversion fails.</returns>
    private object? ApplyArithmeticOperator(object? left, object? right, 
        Func<long, long, long> longOp, 
        Func<double, double, double> doubleOp,
        Func<decimal, decimal, decimal> decimalOp)
    {
        if (left == null || right == null)
            return null;

        if (left is string || right is string)
            return null;

        var targetType = DetermineArithmeticTargetType(left, right);

        return targetType switch
        {
            ArithmeticType.Long => ConvertAndApply(left, right, longOp, TryConvertToInt64NumericOnly),
            ArithmeticType.Double => ConvertAndApply(left, right, doubleOp, TryConvertToDoubleNumericOnly),
            ArithmeticType.Decimal => ConvertAndApply(left, right, decimalOp, TryConvertToDecimalNumericOnly),
            _ => null
        };
    }

    /// <summary>
    /// Enumeration of supported arithmetic target types for type-preserving operations.
    /// Priority order: Decimal (highest precision) > Double (floating-point) > Long (integer default).
    /// </summary>
    private enum ArithmeticType
    {
        /// <summary>Integer arithmetic using 64-bit signed integer (for byte, short, int, long types).</summary>
        Long,
        /// <summary>Floating-point arithmetic using double precision (for float, double types).</summary>
        Double,
        /// <summary>High-precision decimal arithmetic (for decimal type or financial calculations).</summary>
        Decimal
    }

    /// <summary>
    /// Determines the target arithmetic type for an operation based on operand types.
    /// Uses type promotion rules: decimal > double > long.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>The target arithmetic type to use for the operation.</returns>
    private ArithmeticType DetermineArithmeticTargetType(object left, object right)
    {
        if (left is decimal || right is decimal)
            return ArithmeticType.Decimal;
        
        if (left is double || right is double || left is float || right is float)
            return ArithmeticType.Double;
        
        return ArithmeticType.Long;
    }

    /// <summary>
    /// Generic helper that converts both operands to the target type and applies the operation.
    /// Returns null if either operand fails conversion.
    /// </summary>
    /// <typeparam name="T">Target numeric type (long, double, or decimal).</typeparam>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <param name="operation">Operation to apply after conversion.</param>
    /// <param name="converter">Converter function to transform objects to target type.</param>
    /// <returns>Result of the operation, or null if conversion fails.</returns>
    private T? ConvertAndApply<T>(object? left, object? right, Func<T, T, T> operation, Func<object?, T?> converter) where T : struct
    {
        var leftConverted = converter(left);
        var rightConverted = converter(right);

        if (!leftConverted.HasValue || !rightConverted.HasValue)
            return null;

        return operation(leftConverted.Value, rightConverted.Value);
    }

    /// <summary>
    /// Attempts to convert a value to Double, rejecting strings and accepting only boxed numeric types.
    /// Rejects NaN and Infinity values for safety.
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
    /// Runtime greater than operator that handles object &gt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThan(left, right);
    }

    /// <summary>
    /// Runtime less than operator that handles object &lt; object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThan(left, right);
    }

    /// <summary>
    /// Runtime greater than or equal operator that handles object &gt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalGreaterThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.GreaterThanOrEqual(left, right);
    }

    /// <summary>
    /// Runtime less than or equal operator that handles object &lt;= object with automatic type conversion.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalLessThanOrEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.LessThanOrEqual(left, right);
    }

    /// <summary>
    /// Runtime equality operator that handles object == object with automatic type conversion.
    /// For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    /// Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.Equal(left, right);
    }

    /// <summary>
    /// Runtime inequality operator that handles object != object with automatic type conversion.
    /// For strings, tries numeric conversion first. If both convert successfully, compares numerically.
    /// Otherwise compares as strings.
    /// </summary>
    [BindableMethod(true)]
    public bool? InternalNotEqualOperator(object? left, object? right)
    {
        return RuntimeOperators.NotEqual(left, right);
    }

    #endregion
}
