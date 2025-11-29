using System;

namespace Musoq.Plugins.Lib.TypeConversion;

/// <summary>
/// Type converter that rejects strings entirely and only accepts boxed numeric types.
/// Used for arithmetic operations on System.Object columns.
/// </summary>
internal class NumericOnlyTypeConverter
{
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

    /// <inheritdoc />
    public int? TryConvertToInt32(object? value)
    {
        return TryConvertToIntegralTypeNumericOnly(
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
        return TryConvertToIntegralTypeNumericOnly(
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
                catch { return 0m; }
            },
            (_, _) => true);
    }

    public double? TryConvertToDouble(object? value)
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
}
