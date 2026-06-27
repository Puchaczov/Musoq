using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.Helpers;

public static class StrictCastRuntime
{
    public static IReadOnlyList<string> SupportedTypeNames { get; } =
    [
        "Boolean",
        "Byte",
        "SByte",
        "Int16",
        "UInt16",
        "Int32",
        "UInt32",
        "Int64",
        "UInt64",
        "Single",
        "Double",
        "Decimal",
        "Char",
        "String",
        "DateTime",
        "DateTimeOffset",
        "TimeSpan",
        "Guid"
    ];

    public static string SupportedClrTypeNames => string.Join(", ", SupportedTypeNames);

    public static bool TryGetReturnType(string typeName, [NotNullWhen(true)] out Type? returnType)
    {
        returnType = typeName switch
        {
            _ when IsTarget(typeName, "Boolean") => typeof(bool?),
            _ when IsTarget(typeName, "Byte") => typeof(byte?),
            _ when IsTarget(typeName, "SByte") => typeof(sbyte?),
            _ when IsTarget(typeName, "Int16") => typeof(short?),
            _ when IsTarget(typeName, "UInt16") => typeof(ushort?),
            _ when IsTarget(typeName, "Int32") => typeof(int?),
            _ when IsTarget(typeName, "UInt32") => typeof(uint?),
            _ when IsTarget(typeName, "Int64") => typeof(long?),
            _ when IsTarget(typeName, "UInt64") => typeof(ulong?),
            _ when IsTarget(typeName, "Single") => typeof(float?),
            _ when IsTarget(typeName, "Double") => typeof(double?),
            _ when IsTarget(typeName, "Decimal") => typeof(decimal?),
            _ when IsTarget(typeName, "Char") => typeof(char?),
            _ when IsTarget(typeName, "String") => typeof(string),
            _ when IsTarget(typeName, "DateTime") => typeof(DateTime?),
            _ when IsTarget(typeName, "DateTimeOffset") => typeof(DateTimeOffset?),
            _ when IsTarget(typeName, "TimeSpan") => typeof(TimeSpan?),
            _ when IsTarget(typeName, "Guid") => typeof(Guid?),
            _ => null
        };

        return returnType != null;
    }

    public static string CreateUnsupportedTargetMessage(string typeName)
    {
        return $"Cast target '{typeName}' is not supported. Postfix casts support CLR type names only: {SupportedClrTypeNames}.";
    }

    public static bool? ToBoolean(object? value) =>
        IsNull(value) ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    public static byte? ToByte(object? value) =>
        IsNull(value) ? null : Convert.ToByte(value, CultureInfo.InvariantCulture);

    public static sbyte? ToSByte(object? value) =>
        IsNull(value) ? null : Convert.ToSByte(value, CultureInfo.InvariantCulture);

    public static short? ToInt16(object? value) =>
        IsNull(value) ? null : Convert.ToInt16(value, CultureInfo.InvariantCulture);

    public static ushort? ToUInt16(object? value) =>
        IsNull(value) ? null : Convert.ToUInt16(value, CultureInfo.InvariantCulture);

    public static int? ToInt32(object? value) =>
        IsNull(value) ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public static uint? ToUInt32(object? value) =>
        IsNull(value) ? null : Convert.ToUInt32(value, CultureInfo.InvariantCulture);

    public static long? ToInt64(object? value) =>
        IsNull(value) ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);

    public static ulong? ToUInt64(object? value) =>
        IsNull(value) ? null : Convert.ToUInt64(value, CultureInfo.InvariantCulture);

    public static float? ToSingle(object? value) =>
        IsNull(value) ? null : Convert.ToSingle(value, CultureInfo.InvariantCulture);

    public static double? ToDouble(object? value) =>
        IsNull(value) ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);

    public static decimal? ToDecimal(object? value) =>
        IsNull(value) ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    public static char? ToChar(object? value) =>
        IsNull(value) ? null : value is string text ? char.Parse(text) : Convert.ToChar(value, CultureInfo.InvariantCulture);

    public static string? ToString(object? value) =>
        IsNull(value) ? null : Convert.ToString(value, CultureInfo.InvariantCulture);

    public static DateTime? ToDateTime(object? value) =>
        IsNull(value) ? null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);

    public static DateTimeOffset? ToDateTimeOffset(object? value)
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

    public static TimeSpan? ToTimeSpan(object? value)
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

    public static Guid? ToGuid(object? value)
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

    private static bool IsTarget(string actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}
