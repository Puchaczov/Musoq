using System;

namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
///     Type converter that allows precision loss but validates range constraints.
///     Used for comparison operations (&gt;, &lt;, &gt;=, &lt;=) where approximate equality is acceptable.
/// </summary>
internal class ComparisonTypeConverter
{
    private T? TryConvertToIntegralTypeComparison<T>(object? value, Func<object, T> converter, Func<T, bool> rangeCheck)
        where T : struct
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
                    return rangeCheck(uintResult) ? uintResult : null;

                case long longValue:
                    var longResult = converter(longValue);
                    return rangeCheck(longResult) ? longResult : null;

                case ulong ulongValue:
                    var ulongResult = converter(ulongValue);
                    return rangeCheck(ulongResult) ? ulongResult : null;

                case float floatValue:
                    if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                        return null;
                    var floatResult = converter(floatValue);
                    return rangeCheck(floatResult) ? floatResult : null;

                case double doubleValue:
                    if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
                        return null;
                    var doubleResult = converter(doubleValue);
                    return rangeCheck(doubleResult) ? doubleResult : null;

                case decimal decimalValue:
                    var decimalResult = converter(decimalValue);
                    return rangeCheck(decimalResult) ? decimalResult : null;

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

    /// <inheritdoc />
    public int? TryConvertToInt32(object? value)
    {
        return TryConvertToIntegralTypeComparison(
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
            _ =>
            {
                if (value is uint and > int.MaxValue)
                    return false;
                if (value is long and (< int.MinValue or > int.MaxValue))
                    return false;
                if (value is ulong and > int.MaxValue)
                    return false;
                if (value is float and (< int.MinValue or > int.MaxValue))
                    return false;
                if (value is double and (< int.MinValue or > int.MaxValue))
                    return false;
                if (value is decimal and (< int.MinValue or > int.MaxValue))
                    return false;
                return true;
            });
    }

    /// <inheritdoc />
    public long? TryConvertToInt64(object? value)
    {
        return TryConvertToIntegralTypeComparison(
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
            _ =>
            {
                if (value is ulong and > long.MaxValue)
                    return false;
                if (value is float and (< long.MinValue or > long.MaxValue))
                    return false;
                if (value is double and (< long.MinValue or > long.MaxValue))
                    return false;
                if (value is decimal and (< long.MinValue or > long.MaxValue))
                    return false;
                return true;
            });
    }

    /// <inheritdoc />
    public decimal? TryConvertToDecimal(object? value)
    {
        return TryConvertToIntegralTypeComparison(
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
                catch
                {
                    return 0m;
                }
            },
            _ => true);
    }

    /// <inheritdoc />
    public double? TryConvertToDouble(object? value)
    {
        if (value == null)
            return null;

        try
        {
            switch (value)
            {
                case double d:
                    return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
                case float f:
                    return float.IsNaN(f) || float.IsInfinity(f) ? null : (double)f;
                case string stringValue:
                    return double.TryParse(stringValue, out var parsed) && !double.IsNaN(parsed) &&
                           !double.IsInfinity(parsed)
                        ? parsed
                        : null;
                default:
                    var result = Convert.ToDouble(value);
                    return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
            }
        }
        catch
        {
            return null;
        }
    }
}