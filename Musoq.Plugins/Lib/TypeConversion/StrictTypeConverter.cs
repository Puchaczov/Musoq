using System;
using System.Globalization;

namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
/// Type converter that rejects any conversions resulting in precision loss.
/// Used for equality comparisons where exact values matter.
/// </summary>
internal class StrictTypeConverter
{
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

    public int? TryConvertToInt32(object? value)
    {
        return TryConvertToIntegralTypeStrict(
            value,
            obj =>
            {
                try { return Convert.ToInt32(obj); }
                catch { return 0; }
            },
            (_, _) =>
            {
                if (value is uint and > int.MaxValue)
                    return false;
                if (value is long and (< int.MinValue or > int.MaxValue))
                    return false;
                if (value is ulong and > int.MaxValue)
                    return false;
                return true;
            });
    }

    public long? TryConvertToInt64(object? value)
    {
        return TryConvertToIntegralTypeStrict(
            value,
            obj =>
            {
                try { return Convert.ToInt64(obj); }
                catch { return 0L; }
            },
            (_, _) =>
            {
                if (value is ulong and > long.MaxValue)
                    return false;
                return true;
            });
    }

    public decimal? TryConvertToDecimal(object? value)
    {
        return TryConvertToIntegralTypeStrict(
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
            (_, _) => true);
    }

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
                    return double.TryParse(stringValue, out var parsed) && !double.IsNaN(parsed) && !double.IsInfinity(parsed) ? parsed : null;
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
