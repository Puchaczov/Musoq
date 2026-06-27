using System.Globalization;

namespace Musoq.Plugins.Lib.TypeConversion;

internal static class TypeConversionCore
{
    internal static int? TryConvertToInt32(object? value, TypeConversionPolicy policy)
    {
        try
        {
            return value switch
            {
                null => null,
                int directValue => directValue,
                byte byteValue => byteValue,
                sbyte sbyteValue => sbyteValue,
                short shortValue => shortValue,
                ushort ushortValue => ushortValue,
                uint uintValue => uintValue <= int.MaxValue ? Convert.ToInt32(uintValue) : null,
                long longValue => longValue is >= int.MinValue and <= int.MaxValue ? Convert.ToInt32(longValue) : null,
                ulong ulongValue => ulongValue <= int.MaxValue ? Convert.ToInt32(ulongValue) : null,
                float floatValue => ConvertFloatingToInt32(floatValue, policy),
                double doubleValue => ConvertFloatingToInt32(doubleValue, policy),
                decimal decimalValue => ConvertDecimalToInt32(decimalValue, policy),
                string stringValue when AllowsText(policy) => int.TryParse(stringValue, out var parsedInt) ? parsedInt : null,
                bool boolValue when AllowsBoolean(policy) => boolValue ? 1 : 0,
                _ => policy == TypeConversionPolicy.NumericOnly ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return null;
        }
    }

    internal static long? TryConvertToInt64(object? value, TypeConversionPolicy policy)
    {
        try
        {
            return value switch
            {
                null => null,
                long directValue => directValue,
                byte byteValue => byteValue,
                sbyte sbyteValue => sbyteValue,
                short shortValue => shortValue,
                ushort ushortValue => ushortValue,
                int intValue => intValue,
                uint uintValue => uintValue,
                ulong ulongValue => ulongValue <= long.MaxValue ? Convert.ToInt64(ulongValue) : null,
                float floatValue => ConvertFloatingToInt64(floatValue, policy),
                double doubleValue => ConvertFloatingToInt64(doubleValue, policy),
                decimal decimalValue => ConvertDecimalToInt64(decimalValue, policy),
                string stringValue when AllowsText(policy) => TryParseInt64(stringValue, policy, out var parsedLong)
                    ? parsedLong
                    : null,
                bool boolValue when AllowsBoolean(policy) => boolValue ? 1L : 0L,
                _ => policy == TypeConversionPolicy.NumericOnly ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return null;
        }
    }

    internal static decimal? TryConvertToDecimal(object? value, TypeConversionPolicy policy)
    {
        try
        {
            return value switch
            {
                null => null,
                decimal directValue => directValue,
                byte byteValue => byteValue,
                sbyte sbyteValue => sbyteValue,
                short shortValue => shortValue,
                ushort ushortValue => ushortValue,
                int intValue => intValue,
                uint uintValue => uintValue,
                long longValue => longValue,
                ulong ulongValue => ulongValue,
                float floatValue => ConvertFloatingToDecimal(floatValue, policy),
                double doubleValue => ConvertFloatingToDecimal(doubleValue, policy),
                string stringValue when AllowsText(policy) => decimal.TryParse(stringValue, out var parsedDecimal) ? parsedDecimal : null,
                bool boolValue when AllowsBoolean(policy) => boolValue ? 1m : 0m,
                _ => policy == TypeConversionPolicy.NumericOnly ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return null;
        }
    }

    internal static double? TryConvertToDouble(object? value, TypeConversionPolicy policy)
    {
        if (value == null)
            return null;

        if (value is string && !AllowsText(policy))
            return null;

        try
        {
            var result = value switch
            {
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                string stringValue => double.TryParse(stringValue, out var parsed) ? parsed : double.NaN,
                _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
            };

            return double.IsNaN(result) || double.IsInfinity(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    internal static decimal? TryConvertNumericOnly(object? value)
    {
        if (value == null)
            return null;

        var int32Result = TryConvertToInt32(value, TypeConversionPolicy.NumericOnly);
        if (int32Result.HasValue)
            return int32Result.Value;

        var int64Result = TryConvertToInt64(value, TypeConversionPolicy.NumericOnly);
        if (int64Result.HasValue)
            return int64Result.Value;

        return TryConvertToDecimal(value, TypeConversionPolicy.NumericOnly);
    }

    private static int? ConvertFloatingToInt32(float value, TypeConversionPolicy policy)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return null;

        if (policy == TypeConversionPolicy.Comparison && value is < int.MinValue or > int.MaxValue)
            return null;

        var result = Convert.ToInt32(value);
        return policy == TypeConversionPolicy.Comparison ||
               Math.Abs(value - result) <= float.Epsilon
            ? result
            : null;
    }

    private static int? ConvertFloatingToInt32(double value, TypeConversionPolicy policy)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return null;

        if (policy == TypeConversionPolicy.Comparison && value is < int.MinValue or > int.MaxValue)
            return null;

        var result = Convert.ToInt32(value);
        return policy == TypeConversionPolicy.Comparison ||
               Math.Abs(value - result) <= double.Epsilon
            ? result
            : null;
    }

    private static int? ConvertDecimalToInt32(decimal value, TypeConversionPolicy policy)
    {
        if (policy == TypeConversionPolicy.Comparison && value is < int.MinValue or > int.MaxValue)
            return null;

        var result = Convert.ToInt32(value);
        return policy == TypeConversionPolicy.Comparison || value == result ? result : null;
    }

    private static long? ConvertFloatingToInt64(float value, TypeConversionPolicy policy)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return null;

        if (policy == TypeConversionPolicy.Comparison && value is < long.MinValue or > long.MaxValue)
            return null;

        var result = Convert.ToInt64(value);
        return policy == TypeConversionPolicy.Comparison ||
               Math.Abs(value - result) <= float.Epsilon
            ? result
            : null;
    }

    private static long? ConvertFloatingToInt64(double value, TypeConversionPolicy policy)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return null;

        if (policy == TypeConversionPolicy.Comparison && value is < long.MinValue or > long.MaxValue)
            return null;

        var result = Convert.ToInt64(value);
        return policy == TypeConversionPolicy.Comparison ||
               Math.Abs(value - result) <= double.Epsilon
            ? result
            : null;
    }

    private static long? ConvertDecimalToInt64(decimal value, TypeConversionPolicy policy)
    {
        if (policy == TypeConversionPolicy.Comparison && value is < long.MinValue or > long.MaxValue)
            return null;

        var result = Convert.ToInt64(value);
        return policy == TypeConversionPolicy.Comparison || value == result ? result : null;
    }

    private static decimal? ConvertFloatingToDecimal(float value, TypeConversionPolicy policy)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return null;

        var result = Convert.ToDecimal(value);
        return policy != TypeConversionPolicy.NumericOnly ||
               Math.Abs(value - Convert.ToSingle(result)) <= float.Epsilon
            ? result
            : null;
    }

    private static decimal? ConvertFloatingToDecimal(double value, TypeConversionPolicy policy)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return null;

        var result = Convert.ToDecimal(value);
        return policy != TypeConversionPolicy.NumericOnly ||
               Math.Abs(value - Convert.ToDouble(result)) <= double.Epsilon
            ? result
            : null;
    }

    private static bool AllowsText(TypeConversionPolicy policy)
    {
        return policy != TypeConversionPolicy.NumericOnly;
    }

    private static bool AllowsBoolean(TypeConversionPolicy policy)
    {
        return policy != TypeConversionPolicy.NumericOnly;
    }

    private static bool TryParseInt64(string value, TypeConversionPolicy policy, out long result)
    {
        return policy == TypeConversionPolicy.Strict
            ? long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
            : long.TryParse(value, out result);
    }
}
