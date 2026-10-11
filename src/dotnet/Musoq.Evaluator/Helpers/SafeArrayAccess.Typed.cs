using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class SafeArrayAccess
{
    public static TElement? GetTypedListElement<TList, TElement>(TList? values, int? index)
        where TList : IList<TElement>
    {
        if (values is null)
            return default;
        var position = ResolveTypedIndex(index, values.Count);
        return position >= 0 ? values[position] : default;
    }

    public static TElement? GetTypedReadOnlyListElement<TList, TElement>(TList? values, int? index)
        where TList : IReadOnlyList<TElement>
    {
        if (values is null)
            return default;
        var position = ResolveTypedIndex(index, values.Count);
        return position >= 0 ? values[position] : default;
    }

    public static TValue? GetTypedDictionaryValue<TDictionary, TKey, TValue>(TDictionary? values, TKey key)
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        if (values is null || key is null)
            return default;
        return values.TryGetValue(key, out var value) ? value : default;
    }

    public static TValue? GetTypedMutableDictionaryValue<TDictionary, TKey, TValue>(TDictionary? values, TKey key)
        where TDictionary : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        if (values is null || key is null)
            return default;
        return values.TryGetValue(key, out var value) ? value : default;
    }

    public static TValue? GetTypedNullableDictionaryValue<TDictionary, TKey, TValue>(TDictionary? values, TKey? key)
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : struct
    {
        if (!key.HasValue)
            return default;
        return GetTypedDictionaryValue<TDictionary, TKey, TValue>(values, key.Value);
    }

    public static TValue? GetTypedNullableMutableDictionaryValue<TDictionary, TKey, TValue>(TDictionary? values, TKey? key)
        where TDictionary : IDictionary<TKey, TValue>
        where TKey : struct
    {
        if (!key.HasValue)
            return default;
        return GetTypedMutableDictionaryValue<TDictionary, TKey, TValue>(values, key.Value);
    }

    public static char GetTypedStringCharacter(string? value, int? index)
    {
        if (!index.HasValue)
            return default;
        return GetStringCharacter(value, index.Value);
    }

    private static int ResolveTypedIndex(int? index, int count)
    {
        if (count == 0 || !index.HasValue)
            return -1;
        var position = index.Value;
        if (position < 0)
            position = NormalizeIndex(position, count);
        return position < count ? position : -1;
    }
}
