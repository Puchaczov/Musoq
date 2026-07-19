using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Helpers;

internal static class PrimitiveTypeResolver
{
    public static string RemapPrimitiveTypeName(string typeName)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementType = RemapPrimitiveTypeName(typeName[..^2]);
            return $"{elementType}[]";
        }

        if (typeName.EndsWith('?'))
        {
            var baseType = RemapPrimitiveTypeName(typeName[..^1]);
            return $"System.Nullable`1[{baseType}]";
        }

        return (ScriptParameterTypeCatalog.TryResolveScalar(typeName, out var descriptor)
            ? descriptor.ClrType.FullName
            : typeName) ?? throw new InvalidOperationException($"Failed to resolve type name for {typeName}.");
    }

    public static Type? RemapPrimitiveTypeAsNullable(string typeName)
    {
        var resolvedType = ResolveType(typeName);

        if (resolvedType == null)
            return null;

        return MakeNullable(resolvedType);
    }

    public static bool TryResolveDeclarationType(string typeName, [NotNullWhen(true)] out Type? type)
    {
        type = null;

        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        if (typeName.EndsWith("[]?", StringComparison.Ordinal))
            return false;

        if (typeName.EndsWith("[]", StringComparison.Ordinal))
        {
            var elementTypeName = typeName[..^2];
            if (!TryResolveDeclarationType(elementTypeName, out var elementType))
                return false;

            if (!IsValidQueryExpressionType(elementType))
                return false;

            type = elementType.MakeArrayType();
            return true;
        }

        var resolvedType = ResolveType(RemapPrimitiveTypeName(typeName));
        if (resolvedType == null)
            return false;

        type = resolvedType;
        return true;
    }

    public static bool IsPrimitiveType(Type? type)
    {
        if (type == null)
            return false;

        type = StripNullable(type);

        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset);
    }

    public static bool IsValidQueryExpressionType(Type? type)
    {
        if (type == null)
            return false;

        if (type.FullName == typeof(NullNode.NullType).FullName)
            return true;

        if (type.IsArray)
            return false;

        var typeToCheck = StripNullable(type);

        return IsPrimitiveType(typeToCheck) ||
               typeToCheck == typeof(Guid) ||
               typeToCheck == typeof(TimeSpan);
    }

    public static bool IsSupportedCollectionParameterType(Type? type)
    {
        if (type is not { IsArray: true } || type.GetArrayRank() != 1)
            return false;

        return IsValidQueryExpressionType(type.GetElementType());
    }

    public static Type CreateReadOnlyCollectionType(Type elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        return typeof(IReadOnlyList<>).MakeGenericType(elementType);
    }

    public static Type StripNullable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Nullable.GetUnderlyingType(type) ?? type;
    }

    public static Type MakeNullable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (Nullable.GetUnderlyingType(type) != null || !type.IsValueType)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static Type? ResolveType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        return ScriptParameterTypeCatalog.TryResolveScalar(typeName, out var descriptor)
            ? descriptor.ClrType
            : Type.GetType(typeName);
    }
}
