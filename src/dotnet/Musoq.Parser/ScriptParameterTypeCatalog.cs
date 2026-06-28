using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Musoq.Parser;

public static class ScriptParameterTypeCatalog
{
    private static readonly IReadOnlyDictionary<string, ScriptParameterTypeDescriptor> Aliases =
        new Dictionary<string, ScriptParameterTypeDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["byte"] = new("byte", typeof(byte)),
            ["sbyte"] = new("sbyte", typeof(sbyte)),
            ["short"] = new("short", typeof(short)),
            ["int"] = new("int", typeof(int)),
            ["long"] = new("long", typeof(long)),
            ["ushort"] = new("ushort", typeof(ushort)),
            ["uint"] = new("uint", typeof(uint)),
            ["ulong"] = new("ulong", typeof(ulong)),
            ["string"] = new("string", typeof(string)),
            ["char"] = new("char", typeof(char)),
            ["boolean"] = new("bool", typeof(bool)),
            ["bool"] = new("bool", typeof(bool)),
            ["bit"] = new("bool", typeof(bool)),
            ["float"] = new("float", typeof(float)),
            ["double"] = new("double", typeof(double)),
            ["decimal"] = new("decimal", typeof(decimal)),
            ["money"] = new("decimal", typeof(decimal)),
            ["object"] = new("object", typeof(object)),
            ["datetime"] = new("datetime", typeof(DateTime)),
            ["datetimeoffset"] = new("datetimeoffset", typeof(DateTimeOffset)),
            ["timespan"] = new("timespan", typeof(TimeSpan)),
            ["guid"] = new("guid", typeof(Guid))
        };

    public static bool IsKnownScalarTypeName(string value)
    {
        return Aliases.ContainsKey(value);
    }

    public static bool TryResolveScalar(
        string value,
        [NotNullWhen(true)] out ScriptParameterTypeDescriptor? descriptor)
    {
        return Aliases.TryGetValue(value, out descriptor);
    }

    public static string CanonicalizeDeclarationTypeName(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
            return $"{CanonicalizeDeclarationTypeName(typeName[..^2])}[]";

        if (typeName.EndsWith("?", StringComparison.Ordinal))
            return $"{CanonicalizeDeclarationTypeName(typeName[..^1])}?";

        return Aliases.TryGetValue(typeName, out var descriptor)
            ? descriptor.CanonicalName
            : typeName;
    }
}

public sealed record ScriptParameterTypeDescriptor(
    string CanonicalName,
    Type ClrType);
