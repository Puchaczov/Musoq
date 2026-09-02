using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.Helpers;

/// <summary>
///     Provides safe array access methods that return default values instead of throwing exceptions
///     for out-of-bounds access, following SQL semantics (NULL for missing/invalid access)
/// </summary>
public static class SafeArrayAccess
{
    /// <summary>
    ///     Safely access an array element, returning default(T) for out-of-bounds indices
    ///     Supports negative indexing where -1 means last element, -2 second to last, etc.
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    /// <param name="array">The array to access</param>
    /// <param name="index">The index to access (negative indices supported)</param>
    /// <returns>Array element if valid index, default(T) if out-of-bounds or array is null</returns>
    public static T? GetArrayElement<T>(T[]? array, int index)
    {
        return GetListElement(array, index);
    }

    /// <summary>
    ///     Safely access a string character, returning '\0' for out-of-bounds indices
    ///     Supports negative indexing where -1 means last character, -2 second to last, etc.
    /// </summary>
    /// <param name="str">The string to access</param>
    /// <param name="index">The character index to access (negative indices supported)</param>
    /// <returns>Character if valid index, '\0' if out-of-bounds or string is null</returns>
    public static char GetStringCharacter(string? str, int index)
    {
        if (str == null || str.Length == 0)
            return '\0';

        if (index < 0)
            return str[NormalizeIndex(index, str.Length)];

        if (index >= str.Length)
            return '\0';

        return str[index];
    }

    /// <summary>
    ///     Safely access a dictionary value, returning default(TValue) for missing keys
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type</typeparam>
    /// <typeparam name="TValue">Dictionary value type</typeparam>
    /// <param name="dictionary">The dictionary to access</param>
    /// <param name="key">The key to look up</param>
    /// <returns>Value if key exists, default(TValue) if key missing or dictionary is null</returns>
    public static TValue? GetDictionaryValue<TKey, TValue>(Dictionary<TKey, TValue>? dictionary, TKey key)
        where TKey : notnull
    {
        if (dictionary == null || key == null)
            return default;

        return dictionary.TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>
    ///     Safely access a list element, returning default(T) for out-of-bounds indices
    ///     Supports negative indexing where -1 means last element, -2 second to last, etc.
    /// </summary>
    /// <typeparam name="T">List element type</typeparam>
    /// <param name="list">The list to access</param>
    /// <param name="index">The index to access (negative indices supported)</param>
    /// <returns>List element if valid index, default(T) if out-of-bounds or list is null</returns>
    public static T? GetListElement<T>(IList<T>? list, int index)
    {
        if (list == null || list.Count == 0)
            return default;

        if (index < 0)
            return list[NormalizeIndex(index, list.Count)];

        if (index >= list.Count)
            return default;

        return list[index];
    }

    /// <summary>
    ///     Generic safe access for any indexable type using reflection
    /// </summary>
    /// <param name="indexable">The indexable object</param>
    /// <param name="index">The index to access (int for arrays, string for dictionaries, etc.)</param>
    /// <param name="elementType">The expected element type</param>
    /// <returns>Element if valid, default value if out-of-bounds or error</returns>
    public static object? GetIndexedElement(object? indexable, object? index, Type? elementType)
    {
        elementType ??= indexable is null ? null : ResolveElementType(indexable, index);

        if (indexable == null || index == null)
            return GetDefaultValue(elementType);

        try
        {
            return ResolveIndexedElement(indexable, index, elementType);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or
                                        KeyNotFoundException or InvalidCastException)
        {
            return GetDefaultValue(elementType);
        }
    }

    /// <summary>
    ///     Safely resolve a nested property path that may contain array, list, string, or dictionary
    ///     indexers, returning the default value for the requested result type when a path segment is
    ///     missing or out of bounds.
    /// </summary>
    public static object? GetNestedValue(object? value, string propertyPath, Type? resultType)
    {
        return EvaluationHelper.GetNestedValue(value, propertyPath) ?? GetDefaultValue(resultType);
    }

    private static object? ResolveIndexedElement(object indexable, object index, Type? elementType)
    {
        if (indexable is string str && index is int charIndex)
            return GetStringCharacter(str, charIndex);

        if (indexable is Array array && index is int arrayIndex)
            return GetArrayValue(array, arrayIndex, elementType);

        if (index is int listIndex && TryGetListValue(indexable, listIndex, elementType, out var listValue))
            return listValue;

        if (index is string dictKey)
        {
            var (matched, value) = TryGetDictionaryValue(indexable, dictKey, elementType);
            if (matched)
                return value;
        }

        return GetViaIndexer(indexable, index, elementType);
    }

    private static object? GetArrayValue(Array array, int index, Type? elementType)
    {
        if (array.Length == 0)
            return GetDefaultValue(elementType);

        if (index < 0)
            return array.GetValue(NormalizeIndex(index, array.Length));

        if (index >= array.Length)
            return GetDefaultValue(elementType);

        return array.GetValue(index);
    }

    private static bool TryGetListValue(object indexable, int index, Type? elementType, out object? value)
    {
        if (indexable is IList list)
        {
            value = GetListValue(list, index, elementType);
            return true;
        }

        if (!ImplementsGenericList(indexable.GetType(), typeof(IReadOnlyList<>)))
        {
            value = null;
            return false;
        }

        var countProperty = indexable.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
        var indexerProperty = FindIndexer(indexable.GetType(), typeof(int));
        if (countProperty == null || indexerProperty == null || countProperty.GetValue(indexable) is not int count)
        {
            value = null;
            return false;
        }

        if (count == 0 || index >= count)
        {
            value = GetDefaultValue(elementType);
            return true;
        }

        var effectiveIndex = index < 0 ? NormalizeIndex(index, count) : index;
        try
        {
            value = indexerProperty.GetValue(indexable, [effectiveIndex]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException or
                                                       IndexOutOfRangeException)
        {
            value = GetDefaultValue(elementType);
        }

        return true;
    }

    private static object? GetListValue(IList list, int index, Type? elementType)
    {
        if (list.Count == 0 || index >= list.Count)
            return GetDefaultValue(elementType);

        return list[NormalizeIndex(index, list.Count)];
    }

    private static (bool Matched, object? Value) TryGetDictionaryValue(object indexable, string key, Type? elementType)
    {
        var dictType = indexable.GetType();

        if (indexable is IDictionary<string, object> objectDictionary)
            return (true, objectDictionary.TryGetValue(key, out var objectValue)
                ? objectValue
                : GetDefaultValue(elementType));

        if (indexable is IDictionary nonGenericDictionary)
            return (true, nonGenericDictionary.Contains(key)
                ? nonGenericDictionary[key]
                : GetDefaultValue(elementType));

        if (!dictType.IsGenericType && !dictType.GetInterfaces().Any(static i => i.IsGenericType))
            return (false, null);

        var genericDef = dictType.IsGenericType ? dictType.GetGenericTypeDefinition() : null;
        var isDictionary = genericDef == typeof(Dictionary<,>) ||
                           genericDef == typeof(IDictionary<,>) ||
                           dictType.GetInterfaces().Any(i => i.IsGenericType &&
                                                             i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (!isDictionary)
            return (false, null);

        var tryGetValueMethod = dictType.GetMethod("TryGetValue");
        if (tryGetValueMethod == null)
            return (false, null);

        var parameters = new object?[] { key, null };
        var found = (bool)(tryGetValueMethod.Invoke(indexable, parameters) ?? false);
        return (true, found ? parameters[1] : GetDefaultValue(elementType));
    }

    private static object? GetViaIndexer(object indexable, object index, Type? elementType)
    {
        var indexerProperty = FindIndexer(indexable.GetType(), index.GetType());
        if (indexerProperty == null)
            return GetDefaultValue(elementType);

        try
        {
            return indexerProperty.GetValue(indexable, [index]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException
                                                       or IndexOutOfRangeException or KeyNotFoundException or
                                                       ArgumentException)
        {
            return GetDefaultValue(elementType);
        }
    }

    private static Type? ResolveElementType(object indexable, object? index)
    {
        if (indexable is string && (index is null or int))
            return typeof(char);

        if (indexable is Array array && (index is null or int))
            return array.GetType().GetElementType();

        if (index is null or int)
        {
            var listElementType = FindGenericElementType(indexable.GetType(), typeof(IList<>)) ??
                                   FindGenericElementType(indexable.GetType(), typeof(IReadOnlyList<>));
            if (listElementType != null)
                return listElementType;
        }

        if (index is null or string)
        {
            var dictionaryValueType = FindGenericDictionaryValueType(indexable.GetType());
            if (dictionaryValueType != null)
                return dictionaryValueType;
        }

        var indexer = index is null
            ? indexable.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(static property => property.GetIndexParameters().Length == 1)
            : FindIndexer(indexable.GetType(), index.GetType());
        return indexer?.PropertyType;
    }

    private static Type? FindGenericElementType(Type type, Type genericDefinition)
    {
        var candidate = type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition
            ? type
            : type.GetInterfaces().FirstOrDefault(interfaceType =>
                interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericDefinition);
        return candidate?.GetGenericArguments()[0];
    }

    private static Type? FindGenericDictionaryValueType(Type type)
    {
        var candidate = type.IsGenericType &&
                        (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                         type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                         type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            ? type
            : type.GetInterfaces().FirstOrDefault(interfaceType =>
                interfaceType.IsGenericType &&
                (interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                 interfaceType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        return candidate?.GetGenericArguments()[1];
    }

    private static bool ImplementsGenericList(Type type, Type genericDefinition)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition ||
               type.GetInterfaces().Any(interfaceType =>
                   interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericDefinition);
    }

    private static PropertyInfo? FindIndexer(Type type, Type indexType)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property =>
            {
                var parameters = property.GetIndexParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == indexType;
            });
    }

    private static int NormalizeIndex(int index, int length)
    {
        return (index % length + length) % length;
    }

    private static object? GetDefaultValue(Type? type)
    {
        if (type == null)
            return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return null;

        if (!type.IsValueType)
            return null;

        return Activator.CreateInstance(type);
    }
}
