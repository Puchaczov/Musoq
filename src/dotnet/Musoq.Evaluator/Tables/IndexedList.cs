using System.Collections.Generic;

namespace Musoq.Evaluator.Tables;

public abstract class IndexedList<TKey, TValue>
    where TValue : IValue<TKey>
    where TKey : IEquatable<TKey>
{
    private static readonly IReadOnlyList<TValue> EmptyList = Array.Empty<TValue>();

    protected Dictionary<TKey, List<int>> Indexes { get; } = new();
    protected internal List<TValue> Rows { get; } = [];

    public virtual TValue this[int index] => Rows[index];

    public virtual int Count => Rows.Count;

    public virtual IEnumerable<TValue> this[TKey key]
    {
        get
        {
            var indexes = Indexes[key];
            foreach (var index in indexes)
                yield return Rows[index];
        }
    }

    public virtual bool Contains(TValue value)
    {
        return Rows.Contains(value);
    }

    public virtual bool Contains(TValue value, Func<TValue, TValue, bool> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);
        foreach (var row in Rows)
            if (comparer(row, value))
                return true;
        return false;
    }

    public virtual bool Contains(TKey key, TValue value)
    {
        if (Indexes.TryGetValue(key, out var values))
            foreach (var index in values)
                if (Rows[index].Equals(value))
                    return true;
        return false;
    }

    public virtual bool ContainsKey(TKey key)
    {
        return Indexes.ContainsKey(key);
    }

    public virtual bool TryGetIndexedValues(TKey key, out IReadOnlyList<TValue> values)
    {
        if (Indexes.TryGetValue(key, out var matchedIndexes))
        {
            var resultValues = new List<TValue>(matchedIndexes.Count);

            foreach (var rowIndex in matchedIndexes)
                resultValues.Add(Rows[rowIndex]);

            values = resultValues;
            return true;
        }

        values = EmptyList;
        return false;
    }

    protected void AddIndex(TKey index)
    {
        Indexes.Add(index, []);
    }

    protected bool HasIndex(TKey key)
    {
        foreach (var indexesKey in Indexes.Keys)
            if (Equals(indexesKey, key))
                return true;
        return false;
    }

    protected bool HasMatchKey(TKey indexKey, TValue value)
    {
        return value.FitsTheIndex(indexKey);
    }
}
