using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    private static readonly WeakTypeRuntimeCache<bool> HasIndexerCache =
        new(RuntimeCacheOptions.HasIndexerCacheSize);
    private static readonly WeakTypeRuntimeCache<bool> IsIndexableCache =
        new(RuntimeCacheOptions.IsIndexableCacheSize);

    internal static void ClearTypeInspectionCaches()
    {
        HasIndexerCache.Clear();
        IsIndexableCache.Clear();
    }

    /// <summary>
    ///     Finds the closest common parent type between two types in the inheritance hierarchy.
    /// </summary>
    public static Type FindClosestCommonParent(Type first, Type second)
    {
        var type1Ancestors = new HashSet<Type>();
        Type? currentFirst = first;

        while (currentFirst != null)
        {
            type1Ancestors.Add(currentFirst);
            currentFirst = currentFirst.BaseType;
        }

        Type? currentSecond = second;
        while (currentSecond != null)
        {
            if (type1Ancestors.Contains(currentSecond)) return currentSecond;

            currentSecond = currentSecond.BaseType;
        }

        return typeof(object);
    }

    /// <summary>
    ///     Makes a value type nullable, or returns the type as-is if it's already nullable or a reference type.
    /// </summary>
    public static Type MakeTypeNullable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return PrimitiveTypeResolver.MakeNullable(type);
    }

    /// <summary>
    ///     Strips the nullable wrapper from a nullable type, or returns the type as-is if it's not nullable.
    /// </summary>
    public static Type StripNullable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return PrimitiveTypeResolver.StripNullable(type);
    }

    /// <summary>
    ///     Checks if a type has an indexer property (supports array-like access).
    /// </summary>
    public static bool HasIndexer(Type? type)
    {
        if (type is null) return false;

        return HasIndexerCache.GetOrAdd(type, static t =>
            t.GetProperties().Any(f => f.GetIndexParameters().Length > 0));
    }

    /// <summary>
    ///     Checks if a type supports indexing (has an indexer property or is an array).
    /// </summary>
    public static bool IsIndexableType(Type? type)
    {
        if (type == null) return false;

        return IsIndexableCache.GetOrAdd(type, static t =>
        {
            try
            {
                if (t.IsArray)
                    return true;

                if (t == typeof(string))
                    return true;

                return t.GetProperties().Any(p => p.GetIndexParameters().Length > 0);
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is TypeLoadException)
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Checks if a type is a primitive type that cannot have property access.
    /// </summary>
    public static bool IsPrimitiveType(Type? type)
    {
        if (type == null) return false;

        return PrimitiveTypeResolver.IsPrimitiveType(type);
    }

    /// <summary>
    ///     Checks if a type is a valid query expression type.
    ///     Valid types are primitive types (numeric, bool, char), string, decimal, DateTime, DateTimeOffset, Guid, TimeSpan,
    ///     and null.
    ///     Nullable versions of these types are also valid.
    ///     Arrays and complex types (classes, structs) are not valid.
    /// </summary>
    public static bool IsValidQueryExpressionType(Type? type)
    {
        if (type == null) return false;

        return PrimitiveTypeResolver.IsValidQueryExpressionType(type) || (Nullable.GetUnderlyingType(type) ?? type) is { IsGenericType: true } candidate && candidate.GetGenericTypeDefinition() == typeof(Plugins.CorrelatedScalarSubqueryResult<>);
    }

    /// <summary>
    ///     Checks if a column should be included when expanding the star (*) operator.
    ///     Filters out arrays and non-primitive types.
    ///     <para>
    ///         In this context, a "primitive type" is defined by the <see cref="IsPrimitiveType" /> method,
    ///         which returns true for .NET primitive types, as well as <see cref="string" />, <see cref="decimal" />,
    ///         <see cref="DateTime" />, and <see cref="DateTimeOffset" />.
    ///     </para>
    /// </summary>
    public static bool ShouldIncludeColumnInStarExpansion(Type? columnType)
    {
        if (columnType == null) return false;

        if (columnType.IsArray)
            return false;

        var typeToCheck = StripNullable(columnType);

        return IsPrimitiveType(typeToCheck);
    }

    /// <summary>
    ///     Checks if a type is a generic enumerable and returns the element type.
    /// </summary>
    public static bool IsGenericEnumerable(Type? type, [NotNullWhen(true)] out Type? elementType)
    {
        elementType = null;

        if (type == null || !type.IsGenericType) return false;

        var interfaces = type.GetInterfaces().Concat([type]);

        foreach (var interfaceType in interfaces)
        {
            if (!interfaceType.IsGenericType ||
                interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;

            elementType = interfaceType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is an array and returns the element type.
    /// </summary>
    public static bool IsArray(Type? type, [NotNullWhen(true)] out Type? elementType)
    {
        elementType = null;

        if (type == null || !type.IsArray) return false;

        elementType = type.GetElementType();
        return elementType != null;
    }

    /// <summary>
}
