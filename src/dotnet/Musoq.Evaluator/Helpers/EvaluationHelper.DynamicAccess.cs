using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static readonly WeakTypeRuntimeCache<BoundedRuntimeCache<string, Func<object?, object?>>> NestedValueAccessors =
        new(RuntimeCacheOptions.DynamicAccessorCacheSize);
    private static readonly object MissingNestedValue = new();

    internal static void ClearNestedValueAccessorCache() => NestedValueAccessors.Clear();

    internal static int GetNestedValueAccessorCacheCount(Type type)
    {
        return NestedValueAccessors.TryGetValue(type, out var cache)
            ? cache.Count
            : 0;
    }

    public static object? CreateNullableHashJoinKey(params object?[]? parts)
    {
        if (parts == null)
            return null;

        var keyParts = new object[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (parts[index] is not { } part)
                return null;

            keyParts[index] = part;
        }

        return WindowFunctionHelpers.CompositeKey(keyParts);
    }

    public static IEnumerable<T> WrapScalarForCrossApply<T>(T? value) where T : class
    {
        if (value == null)
            return [];

        return [value];
    }

    public static object? GetNestedValue(object? value, string columnPath)
    {
        ArgumentNullException.ThrowIfNull(columnPath);
        if (value is null || string.IsNullOrWhiteSpace(columnPath))
            return value;

        if (CanUseCachedClrNestedValueAccessor(value.GetType(), columnPath))
        {
            var accessor = GetNestedValueAccessor(value.GetType(), columnPath);
            var cachedValue = accessor(value);
            if (!ReferenceEquals(cachedValue, MissingNestedValue))
                return cachedValue;
        }

        var segments = columnPath.Split('.');
        if (TryResolvePathFromObject(value, segments, out var resolvedValue))
            return resolvedValue;

        throw new InvalidOperationException($"Column with name {columnPath} does not exist in value of type {value.GetType().FullName}.");
    }

    public static Func<object?, object?> GetNestedValueAccessor(Type type, string columnPath)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(columnPath);
        if (string.IsNullOrWhiteSpace(columnPath) ||
            !CanUseCachedClrNestedValueAccessor(type, columnPath))
        {
            return value => GetNestedValue(value, columnPath);
        }

        var accessorsForType = NestedValueAccessors.GetOrAdd(
            type,
            static _ => new BoundedRuntimeCache<string, Func<object?, object?>>(
                RuntimeCacheOptions.DynamicAccessorCacheSize,
                StringComparer.Ordinal));

        return accessorsForType.GetOrAdd(
            columnPath,
            path => CreateClrNestedValueAccessor(type, path));
    }

    private static bool CanUseCachedClrNestedValueAccessor(Type type, string columnPath)
    {
        return !columnPath.Contains('[', StringComparison.Ordinal) &&
               !typeof(IDictionary<string, object>).IsAssignableFrom(type) &&
               !typeof(IDictionary).IsAssignableFrom(type) &&
               !typeof(System.Dynamic.DynamicObject).IsAssignableFrom(type);
    }

    private static Func<object?, object?> CreateClrNestedValueAccessor(Type type, string columnPath)
    {
        var segments = columnPath.Split('.');
        var properties = new PropertyInfo[segments.Length];
        var currentType = type;

        for (var index = 0; index < segments.Length; index++)
        {
            var property = currentType.GetProperty(segments[index], BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
                return _ => MissingNestedValue;

            properties[index] = property;
            currentType = property.PropertyType;
        }

        var getters = new Func<object?, object?>[properties.Length];
        for (var index = 0; index < properties.Length; index++)
            getters[index] = CreateClrPropertyGetter(properties[index]);

        return source =>
        {
            object? current = source;
            foreach (var getter in getters)
            {
                if (current is null)
                    return null;

                current = getter(current);
            }

            return current;
        };
    }

    private static Func<object?, object?> CreateClrPropertyGetter(PropertyInfo property)
    {
        try
        {
            var source = Expression.Parameter(typeof(object), "source");
            var typedSource = Expression.Convert(source, property.DeclaringType!);
            var propertyAccess = Expression.Property(typedSource, property);
            var boxedValue = Expression.Convert(propertyAccess, typeof(object));
            return Expression.Lambda<Func<object?, object?>>(boxedValue, source).Compile();
        }
        catch
        {
            return property.GetValue;
        }
    }

    private static bool TryResolvePathFromObject(object? current, string[] pathSegments, out object? value)
    {
        foreach (var rawSegment in pathSegments)
        {
            if (current is null)
            {
                value = null;
                return true;
            }

            var (segment, indexer) = ParseSegmentIndexer(rawSegment);

            if (segment.Length == 0 && indexer is not null)
            {
                current = ApplyIndexer(current, indexer);
                continue;
            }

            if (current is IDictionary<string, object> dictionary)
            {
                if (!dictionary.TryGetValue(segment, out var dictionaryValue))
                {
                    value = null;
                    return false;
                }

                current = dictionaryValue;
            }
            else if (current is System.Dynamic.DynamicObject dynamicObject)
            {
                if (!TryInvokeDynamicGetMember(dynamicObject, segment, out var dynamicValue))
                {
                    value = null;
                    return false;
                }

                current = dynamicValue;
            }
            else
            {
                var property = current.GetType().GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
                if (property is null)
                {
                    value = null;
                    return false;
                }

                current = property.GetValue(current);
            }

            if (indexer is not null)
                current = ApplyIndexer(current, indexer);
        }

        value = current;
        return true;
    }

    /// <summary>
    /// Parses a path segment that may carry a trailing indexer such as <c>Array[0]</c> or
    /// <c>Dict['key']</c>. The base name is returned together with the index value (boxed int for
    /// numeric indexers, string for key indexers, or <c>null</c> when the segment has no indexer).
    /// </summary>
    private static (string BaseName, object? Indexer) ParseSegmentIndexer(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return (segment, null);

        var openBracket = segment.IndexOf('[', StringComparison.Ordinal);
        if (openBracket < 0 || segment[^1] != ']')
            return (segment, null);

        var baseName = segment[..openBracket];
        var indexerText = segment[(openBracket + 1)..^1];

        if (indexerText is ['\'', _, ..] && indexerText[^1] == '\'')
            return (baseName, indexerText[1..^1]);

        if (int.TryParse(indexerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericIndex))
            return (baseName, numericIndex);

        return (segment, null);
    }

    private static object? ApplyIndexer(object? target, object indexer)
    {
        if (target is null)
            return null;

        if (indexer is string keyIndexer)
        {
            if (target is IDictionary<string, object> stringDictionary)
                return stringDictionary.TryGetValue(keyIndexer, out var value) ? value : null;

            if (target is IDictionary nonGenericDictionary)
                return nonGenericDictionary.Contains(keyIndexer) ? nonGenericDictionary[keyIndexer] : null;

            var keyIndexerProperty = target.GetType()
                .GetProperty("Item", BindingFlags.Public | BindingFlags.Instance, null, null, [typeof(string)], null);
            return keyIndexerProperty?.GetValue(target, [keyIndexer]);
        }

        if (indexer is int numericIndexer)
        {
            if (target is Array array)
                return numericIndexer >= 0 && numericIndexer < array.Length ? array.GetValue(numericIndexer) : null;

            if (target is IList list)
                return numericIndexer >= 0 && numericIndexer < list.Count ? list[numericIndexer] : null;

            var numericIndexerProperty = target.GetType()
                .GetProperty("Item", BindingFlags.Public | BindingFlags.Instance, null, null, [typeof(int)], null);
            return numericIndexerProperty?.GetValue(target, [numericIndexer]);
        }

        return null;
    }

    private static bool TryInvokeDynamicGetMember(System.Dynamic.DynamicObject dynamicObject, string memberName, out object? value)
    {
        try
        {
            if (dynamicObject.TryGetMember(new SimpleGetMemberBinder(memberName), out value))
                return true;
        }
        catch (RuntimeBinderException)
        {
            // A dynamic binder miss is treated as an unresolved member.
        }
        catch (MissingMemberException)
        {
            // Fall through — member is unresolved.
        }

        value = null;
        return false;
    }

    private sealed class SimpleGetMemberBinder(string name) : System.Dynamic.GetMemberBinder(name, ignoreCase: false)
    {
        public override System.Dynamic.DynamicMetaObject FallbackGetMember(
            System.Dynamic.DynamicMetaObject target,
            System.Dynamic.DynamicMetaObject? errorSuggestion)
        {
            return errorSuggestion ?? new System.Dynamic.DynamicMetaObject(
                Expression.Constant(null),
                System.Dynamic.BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
        }
    }

}
