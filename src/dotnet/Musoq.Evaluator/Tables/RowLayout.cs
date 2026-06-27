using System.Collections.Frozen;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

public sealed class RowLayout
{
    private readonly FrozenDictionary<string, int> _indexesByName;

    public RowLayout(int count, IReadOnlyDictionary<string, int> indexesByName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentNullException.ThrowIfNull(indexesByName);

        Count = count;
        _indexesByName = FreezeIndexes(indexesByName);
    }

    public int Count { get; }

    public static RowLayout Create(int count, params RowLayoutName[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var indexesByName = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (name.Index < 0 || name.Index >= count)
                throw new ArgumentOutOfRangeException(nameof(names), $"Column name '{name.Name}' points to invalid index {name.Index}.");

            indexesByName.TryAdd(name.Name, name.Index);
        }

        return new RowLayout(count, indexesByName);
    }

    private static FrozenDictionary<string, int> FreezeIndexes(IReadOnlyDictionary<string, int> indexesByName)
    {
        return indexesByName switch
        {
            FrozenDictionary<string, int> frozen => frozen,
            Dictionary<string, int> dictionary => dictionary.ToFrozenDictionary(dictionary.Comparer),
            _ => indexesByName.ToFrozenDictionary()
        };
    }

    public int GetIndex(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_indexesByName.TryGetValue(name, out var index))
            return index;

        throw new KeyNotFoundException(name);
    }

    public bool HasColumn(string name)
    {
        return name != null && _indexesByName.ContainsKey(name);
    }
}
