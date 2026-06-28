using Musoq.Evaluator.Helpers;
using Musoq.Parser;

namespace Musoq.Evaluator;

public sealed record ScriptParameterContract(
    string Name,
    string DeclaredTypeName,
    string CanonicalTypeName,
    Type ClrType,
    bool IsNullable,
    bool IsCollection,
    Type? ElementClrType,
    string? ElementCanonicalTypeName,
    bool HasDefaultValue,
    ScriptParameterDefaultKind DefaultKind,
    object? DefaultValue)
{
    public static ScriptParameterContract Create(
        string name,
        string declaredTypeName,
        Type clrType,
        bool hasDefaultValue,
        object? defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(declaredTypeName);
        ArgumentNullException.ThrowIfNull(clrType);

        var isCollection = clrType.IsArray;
        var elementClrType = isCollection
            ? clrType.GetElementType() ?? throw new ArgumentException("Collection parameter type must expose an element type.", nameof(clrType))
            : null;
        var defaultKind = !hasDefaultValue
            ? ScriptParameterDefaultKind.None
            : defaultValue == null
                ? ScriptParameterDefaultKind.Null
                : ScriptParameterDefaultKind.Literal;

        return new ScriptParameterContract(
            name,
            declaredTypeName,
            ScriptParameterTypeCatalog.CanonicalizeDeclarationTypeName(declaredTypeName),
            clrType,
            Nullable.GetUnderlyingType(clrType) != null,
            isCollection,
            elementClrType,
            elementClrType != null ? GetCanonicalClrTypeName(elementClrType) : null,
            hasDefaultValue,
            defaultKind,
            defaultValue);
    }

    public static ScriptParameterContract FromLegacy(
        string name,
        Type clrType,
        bool hasDefaultValue,
        object? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        return Create(
            name,
            GetCanonicalClrTypeName(clrType),
            clrType,
            hasDefaultValue,
            defaultValue);
    }

    private static string GetCanonicalClrTypeName(Type type)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType() ??
                              throw new ArgumentException("Array type must expose an element type.", nameof(type));
            return $"{GetCanonicalClrTypeName(elementType)}[]";
        }

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return $"{GetCanonicalClrTypeName(nullableType)}?";

        if (type == typeof(byte))
            return "byte";
        if (type == typeof(sbyte))
            return "sbyte";
        if (type == typeof(short))
            return "short";
        if (type == typeof(int))
            return "int";
        if (type == typeof(long))
            return "long";
        if (type == typeof(ushort))
            return "ushort";
        if (type == typeof(uint))
            return "uint";
        if (type == typeof(ulong))
            return "ulong";
        if (type == typeof(string))
            return "string";
        if (type == typeof(char))
            return "char";
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(float))
            return "float";
        if (type == typeof(double))
            return "double";
        if (type == typeof(decimal))
            return "decimal";
        if (type == typeof(DateTime))
            return "datetime";
        if (type == typeof(DateTimeOffset))
            return "datetimeoffset";
        if (type == typeof(TimeSpan))
            return "timespan";
        if (type == typeof(Guid))
            return "guid";

        return EvaluationHelper.GetCastableType(type);
    }
}
