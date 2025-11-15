using System;
using System.Globalization;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
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
    [BindableMethod]
    public int? TryConvertToInt32Strict(object? value)
    {
        return TryConvertToIntegralTypeStrict<int>(
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
    [BindableMethod]
    public long? TryConvertToInt64Strict(object? value)
    {
        return TryConvertToIntegralTypeStrict<long>(
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
    [BindableMethod]
    public decimal? TryConvertToDecimalStrict(object? value)
    {
        return TryConvertToIntegralTypeStrict<decimal>(
            value,
            obj =>
            {
                try
                {
                    if (obj is float floatValue && (float.IsNaN(floatValue) || float.IsInfinity(floatValue)))
                        throw new InvalidOperationException();
                    if (obj is double doubleValue && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)))
                        throw new InvalidOperationException();
                    return Convert.ToDecimal(obj);
                }
                catch { return 0m; }
            },
            (result, _) => true);
    }

    #endregion

    #region Comparison-Mode Conversions (Allow Lossy Conversions)

    /// <summary>
    /// Generic helper method for comparison-mode type conversions that allow precision loss but validate range constraints.
    /// </summary>
    /// <typeparam name="T">The target numeric type (int, long, or decimal).</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="converter">Function to perform the conversion to type T.</param>
    /// <param name="rangeCheck">Function to validate that the value is within the acceptable range for type T.</param>
    /// <returns>The converted value if successful and within range; otherwise, null.</returns>
    /// <remarks>
    /// This conversion mode allows lossy conversions (e.g., 3.14 → 3) but still validates range constraints.
    /// Used primarily for comparison operations where approximate equality is acceptable.
    /// </remarks>
    private T? TryConvertToIntegralTypeComparison<T>(object? value, Func<object, T> converter, Func<T, bool> rangeCheck) where T : struct
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
                    return rangeCheck(uintResult) ? uintResult : (T?)null;
                
                case long longValue:
                    var longResult = converter(longValue);
                    return rangeCheck(longResult) ? longResult : (T?)null;
                
                case ulong ulongValue:
                    var ulongResult = converter(ulongValue);
                    return rangeCheck(ulongResult) ? ulongResult : (T?)null;
                
                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;
                    var floatResult = converter(floatValue);
                    return rangeCheck(floatResult) ? floatResult : (T?)null;
                
                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;
                    var doubleResult = converter(doubleValue);
                    return rangeCheck(doubleResult) ? doubleResult : (T?)null;
                
                case decimal decimalValue:
                    var decimalResult = converter(decimalValue);
                    return rangeCheck(decimalResult) ? decimalResult : (T?)null;
                
                case string stringValue:
                    if (typeof(T) == typeof(int) && int.TryParse(stringValue, out var parsedInt))
                        return (T)(object)parsedInt;
                    if (typeof(T) == typeof(long) && long.TryParse(stringValue, out var parsedLong))
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
    [BindableMethod]
    public int? TryConvertToInt32Comparison(object? value)
    {
        return TryConvertToIntegralTypeComparison<int>(
            value,
            obj =>
            {
                try { return Convert.ToInt32(obj); }
                catch { return 0; }
            },
            result =>
            {
                if (value is uint uintValue && uintValue > int.MaxValue)
                    return false;
                if (value is long longValue && (longValue < int.MinValue || longValue > int.MaxValue))
                    return false;
                if (value is ulong ulongValue && ulongValue > int.MaxValue)
                    return false;
                if (value is float floatValue && (floatValue < int.MinValue || floatValue > int.MaxValue))
                    return false;
                if (value is double doubleValue && (doubleValue < int.MinValue || doubleValue > int.MaxValue))
                    return false;
                if (value is decimal decimalValue && (decimalValue < int.MinValue || decimalValue > int.MaxValue))
                    return false;
                return true;
            });
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
    [BindableMethod]
    public long? TryConvertToInt64Comparison(object? value)
    {
        return TryConvertToIntegralTypeComparison<long>(
            value,
            obj =>
            {
                try { return Convert.ToInt64(obj); }
                catch { return 0L; }
            },
            result =>
            {
                if (value is ulong ulongValue && ulongValue > long.MaxValue)
                    return false;
                if (value is float floatValue && (floatValue < long.MinValue || floatValue > long.MaxValue))
                    return false;
                if (value is double doubleValue && (doubleValue < long.MinValue || doubleValue > long.MaxValue))
                    return false;
                if (value is decimal decimalValue && (decimalValue < long.MinValue || decimalValue > long.MaxValue))
                    return false;
                return true;
            });
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
    [BindableMethod]
    public decimal? TryConvertToDecimalComparison(object? value)
    {
        return TryConvertToIntegralTypeComparison<decimal>(
            value,
            obj =>
            {
                try
                {
                    if (obj is float floatValue && (float.IsNaN(floatValue) || float.IsInfinity(floatValue)))
                        throw new InvalidOperationException();
                    if (obj is double doubleValue && (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)))
                        throw new InvalidOperationException();
                    return Convert.ToDecimal(obj);
                }
                catch { return 0m; }
            },
            result => true);
    }

    #endregion
}
