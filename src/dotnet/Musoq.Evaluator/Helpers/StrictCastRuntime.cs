using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Evaluator.Helpers;

public static partial class StrictCastRuntime
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
        => StrictCastConstantValidation.TryValidate(canonicalTypeName, value, out error);

    public static bool TryGetReturnType(string typeName, [NotNullWhen(true)] out Type? returnType)
    {
        return TryResolveTarget(typeName, out _, out returnType);
    }

    public static string CreateUnsupportedTargetMessage(string typeName)
    {
        return $"Cast target '{typeName}' is not supported. Postfix casts support CLR type names and C# aliases only: {SupportedClrTypeNames}; aliases: {SupportedCSharpAliasNamesText}.";
    }

}
