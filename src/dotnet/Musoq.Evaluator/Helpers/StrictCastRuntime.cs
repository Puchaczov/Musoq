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

    private static IReadOnlyList<string> SupportedCSharpAliasNames { get; } =
    [
        "bool",
        "byte",
        "sbyte",
        "short",
        "ushort",
        "int",
        "uint",
        "long",
        "ulong",
        "float",
        "double",
        "decimal",
        "char",
        "string"
    ];

    public static string SupportedClrTypeNames => string.Join(", ", SupportedTypeNames);

    private static string SupportedCSharpAliasNamesText => string.Join(", ", SupportedCSharpAliasNames);

    private static readonly IReadOnlyDictionary<string, CastTarget> CastTargets =
        new Dictionary<string, CastTarget>(StringComparer.OrdinalIgnoreCase)
        {
            ["Boolean"] = new("Boolean", typeof(bool?)),
            ["bool"] = new("Boolean", typeof(bool?)),
            ["Byte"] = new("Byte", typeof(byte?)),
            ["byte"] = new("Byte", typeof(byte?)),
            ["SByte"] = new("SByte", typeof(sbyte?)),
            ["sbyte"] = new("SByte", typeof(sbyte?)),
            ["Int16"] = new("Int16", typeof(short?)),
            ["short"] = new("Int16", typeof(short?)),
            ["UInt16"] = new("UInt16", typeof(ushort?)),
            ["ushort"] = new("UInt16", typeof(ushort?)),
            ["Int32"] = new("Int32", typeof(int?)),
            ["int"] = new("Int32", typeof(int?)),
            ["UInt32"] = new("UInt32", typeof(uint?)),
            ["uint"] = new("UInt32", typeof(uint?)),
            ["Int64"] = new("Int64", typeof(long?)),
            ["long"] = new("Int64", typeof(long?)),
            ["UInt64"] = new("UInt64", typeof(ulong?)),
            ["ulong"] = new("UInt64", typeof(ulong?)),
            ["Single"] = new("Single", typeof(float?)),
            ["float"] = new("Single", typeof(float?)),
            ["Double"] = new("Double", typeof(double?)),
            ["double"] = new("Double", typeof(double?)),
            ["Decimal"] = new("Decimal", typeof(decimal?)),
            ["decimal"] = new("Decimal", typeof(decimal?)),
            ["Char"] = new("Char", typeof(char?)),
            ["char"] = new("Char", typeof(char?)),
            ["String"] = new("String", typeof(string)),
            ["string"] = new("String", typeof(string)),
            ["DateTime"] = new("DateTime", typeof(DateTime?)),
            ["DateTimeOffset"] = new("DateTimeOffset", typeof(DateTimeOffset?)),
            ["TimeSpan"] = new("TimeSpan", typeof(TimeSpan?)),
            ["Guid"] = new("Guid", typeof(Guid?))
        };

    private readonly record struct CastTarget(string CanonicalName, Type ReturnType);

    internal static bool TryResolveTarget(
        string typeName,
        out string canonicalTypeName,
        [NotNullWhen(true)] out Type? returnType)
    {
        if (CastTargets.TryGetValue(typeName, out var target))
        {
            canonicalTypeName = target.CanonicalName;
            returnType = target.ReturnType;
            return true;
        }

        canonicalTypeName = string.Empty;
        returnType = null;
        return false;
    }

    internal static bool TryValidateConstant(string canonicalTypeName, object? value, out string error)
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

    public static bool TryGetReturnType(string typeName, [NotNullWhen(true)] out Type? returnType)
    {
        return TryResolveTarget(typeName, out _, out returnType);
    }

    public static string CreateUnsupportedTargetMessage(string typeName)
    {
        return $"Cast target '{typeName}' is not supported. Postfix casts support CLR type names and C# aliases only: {SupportedClrTypeNames}; aliases: {SupportedCSharpAliasNamesText}.";
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

    public static float? ToSingle(float value) => value;

    public static float? ToSingle(float? value) => value;

    public static float? ToSingle(double value) => Convert.ToSingle(value);

    public static float? ToSingle(double? value) =>
        value.HasValue ? Convert.ToSingle(value.GetValueOrDefault()) : null;

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

}
