using System.Globalization;

namespace Musoq.Evaluator.Helpers;

/// <summary>Validates compiler-owned literal metadata; never invoked by generated execution.</summary>
internal static class StrictCastConstantValidation
{
    internal static bool TryValidate(string canonicalTypeName, object? value, out string error)
    {
        try
        {
            switch (canonicalTypeName)
            {
                case "Boolean": _ = ToBoolean(value); break;
                case "Byte": _ = ToByte(value); break;
                case "SByte": _ = ToSByte(value); break;
                case "Int16": _ = ToInt16(value); break;
                case "UInt16": _ = ToUInt16(value); break;
                case "Int32": _ = ToInt32(value); break;
                case "UInt32": _ = ToUInt32(value); break;
                case "Int64": _ = ToInt64(value); break;
                case "UInt64": _ = ToUInt64(value); break;
                case "Single": _ = ToSingle(value); break;
                case "Double": _ = ToDouble(value); break;
                case "Decimal": _ = ToDecimal(value); break;
                case "Char": _ = ToChar(value); break;
                case "String": _ = ToString(value); break;
                case "DateTime": _ = ToDateTime(value); break;
                case "DateTimeOffset": _ = ToDateTimeOffset(value); break;
                case "TimeSpan": _ = ToTimeSpan(value); break;
                case "Guid": _ = ToGuid(value); break;
                default: throw new InvalidOperationException($"Unknown canonical cast target '{canonicalTypeName}'.");
            }

            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidCastException or OverflowException)
        {
            error = exception.Message;
            return false;
        }
    }

    private static bool? ToBoolean(object? value) =>
        IsNull(value) ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    private static byte? ToByte(object? value) =>
        IsNull(value) ? null : Convert.ToByte(value, CultureInfo.InvariantCulture);

    private static sbyte? ToSByte(object? value) =>
        IsNull(value) ? null : Convert.ToSByte(value, CultureInfo.InvariantCulture);

    private static short? ToInt16(object? value) =>
        IsNull(value) ? null : Convert.ToInt16(value, CultureInfo.InvariantCulture);

    private static ushort? ToUInt16(object? value) =>
        IsNull(value) ? null : Convert.ToUInt16(value, CultureInfo.InvariantCulture);

    private static int? ToInt32(object? value) =>
        IsNull(value) ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    private static uint? ToUInt32(object? value) =>
        IsNull(value) ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);

    private static long? ToInt64(object? value) =>
        IsNull(value) ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);

    private static ulong? ToUInt64(object? value) =>
        IsNull(value) ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);

    private static float? ToSingle(float value) => value;

    private static float? ToSingle(float? value) => value;

    private static float? ToSingle(double value) => Convert.ToSingle(value);

    private static float? ToSingle(double? value) =>
        value.HasValue ? Convert.ToSingle(value.GetValueOrDefault()) : null;

    private static float? ToSingle(object? value) =>
        IsNull(value) ? null : Convert.ToSingle(value, CultureInfo.InvariantCulture);

    private static double? ToDouble(object? value) =>
        IsNull(value) ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);

    private static decimal? ToDecimal(object? value) =>
        IsNull(value) ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private static char? ToChar(object? value) =>
        IsNull(value) ? null : value is string text ? char.Parse(text) : Convert.ToChar(value, CultureInfo.InvariantCulture);

    private static string? ToString(object? value) =>
        IsNull(value) ? null : Convert.ToString(value, CultureInfo.InvariantCulture);

    private static DateTime? ToDateTime(object? value) =>
        IsNull(value) ? null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);

    private static DateTimeOffset? ToDateTimeOffset(object? value)
    {
        if (IsNull(value))
            return null;

        var source = value!;
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot cast value of type '{source.GetType().Name}' to DateTimeOffset.")
        };
    }

    private static TimeSpan? ToTimeSpan(object? value)
    {
        if (IsNull(value))
            return null;

        var source = value!;
        return value switch
        {
            TimeSpan timeSpan => timeSpan,
            string text => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Cannot cast value of type '{source.GetType().Name}' to TimeSpan.")
        };
    }

    private static Guid? ToGuid(object? value)
    {
        if (IsNull(value))
            return null;

        var source = value!;
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => throw new InvalidCastException($"Cannot cast value of type '{source.GetType().Name}' to Guid.")
        };
    }

    private static bool IsNull(object? value)
    {
        return value is null or DBNull;
    }

}
