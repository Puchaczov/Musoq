using System.Globalization;

namespace Musoq.Evaluator.Visitors;

internal static class ScriptValueConverter
{
    public static ScriptValueConversionResult ConvertValue(
        string symbolKind,
        string name,
        string declaredTypeName,
        Type targetType,
        object? rawValue)
    {
        if (rawValue == null)
        {
            return CanAcceptNull(targetType)
                ? ScriptValueConversionResult.Converted(null)
                : ScriptValueConversionResult.Failed(
                    $"{symbolKind} '{name}' has a null value but type '{declaredTypeName}' is not nullable.");
        }

        var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (conversionType.IsInstanceOfType(rawValue))
            return ScriptValueConversionResult.Converted(rawValue);

        if (!TryConvert(rawValue, conversionType, out var converted))
        {
            return ScriptValueConversionResult.Failed(
                $"{symbolKind} '{name}' value '{rawValue}' cannot be converted to '{declaredTypeName}'.");
        }

        return ScriptValueConversionResult.Converted(converted);
    }

    private static bool CanAcceptNull(Type targetType)
    {
        return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
    }

    private static bool TryConvert(object rawValue, Type targetType, out object? value)
    {
        value = null;

        if (rawValue is string stringValue)
            return TryConvertString(stringValue, targetType, out value);

        if (targetType == typeof(string) || targetType == typeof(bool))
            return false;

        if (!IsNumericType(targetType) || rawValue is not IConvertible)
            return false;

        try
        {
            value = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            value = null;
            return false;
        }
    }

    private static bool TryConvertString(string value, Type targetType, out object? converted)
    {
        converted = null;

        if (targetType == typeof(string))
        {
            converted = value;
            return true;
        }

        if (targetType == typeof(char) && value.Length == 1)
        {
            converted = value[0];
            return true;
        }

        if (targetType == typeof(Guid) && Guid.TryParse(value, out var guid))
        {
            converted = guid;
            return true;
        }

        if (targetType == typeof(DateTime) &&
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
        {
            converted = dateTime;
            return true;
        }

        if (targetType == typeof(DateTimeOffset) &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
        {
            converted = dateTimeOffset;
            return true;
        }

        if (targetType == typeof(TimeSpan) &&
            TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var timeSpan))
        {
            converted = timeSpan;
            return true;
        }

        return false;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte)
               || type == typeof(sbyte)
               || type == typeof(short)
               || type == typeof(ushort)
               || type == typeof(int)
               || type == typeof(uint)
               || type == typeof(long)
               || type == typeof(ulong)
               || type == typeof(float)
               || type == typeof(double)
               || type == typeof(decimal);
    }
}