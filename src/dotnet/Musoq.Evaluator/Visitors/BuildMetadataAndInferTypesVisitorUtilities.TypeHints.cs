using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    private static readonly Dictionary<Type, DynamicObjectPropertyTypeHintAttribute[]> TypeHintAttributeCache = new();

    internal static DynamicObjectPropertyTypeHintAttribute[] GetCachedTypeHintAttributes(Type type)
    {
        lock (TypeHintAttributeCache)
        {
            if (TypeHintAttributeCache.TryGetValue(type, out var cached))
                return cached;

            var attributes = type.GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>().ToArray();
            TypeHintAttributeCache[type] = attributes;
            return attributes;
        }
    }
}
