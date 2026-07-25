using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Runtime;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    internal static string? GetArrayElementIntendedTypeName(string? arrayIntendedTypeName)
    {
        if (string.IsNullOrEmpty(arrayIntendedTypeName))
            return null;

        if (arrayIntendedTypeName.EndsWith("[]", StringComparison.Ordinal))
            return arrayIntendedTypeName.Substring(0, arrayIntendedTypeName.Length - 2);

        return arrayIntendedTypeName;
    }

    private static readonly WeakTypeRuntimeCache<DynamicObjectPropertyTypeHintAttribute[]> TypeHintAttributeCache =
        new(RuntimeCacheOptions.TypeHintAttributeCacheSize);

    internal static DynamicObjectPropertyTypeHintAttribute[] GetCachedTypeHintAttributes(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return TypeHintAttributeCache.GetOrAdd(
            type,
            static candidate => candidate.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray());
    }
}
