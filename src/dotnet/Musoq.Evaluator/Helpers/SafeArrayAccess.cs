using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.Helpers;

/// <summary>
///     Provides safe array access methods that return default values instead of throwing exceptions
///     for out-of-bounds access, following SQL semantics (NULL for missing/invalid access)
/// </summary>
public static partial class SafeArrayAccess
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

    public static T? GetReadOnlyListElement<T>(IReadOnlyList<T>? values, int index)
    {
        if (values == null || values.Count == 0)
            return default;

        var normalized = index < 0 ? NormalizeIndex(index, values.Count) : index;
        return normalized < values.Count ? values[normalized] : default;
    }

    private static int NormalizeIndex(int index, int length)
        => (index % length + length) % length;
}
