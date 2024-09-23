using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tables;

public abstract class IndexedList<TKey, TValue>
    where TValue : IValue<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly Dictionary<TKey, List<int>> Indexes;
    protected internal readonly List<TValue> Rows;

    protected IndexedList()
    {
        Indexes = new Dictionary<TKey, List<int>>();
        Rows = new List<TValue>();
    }

    public TValue this[int index] => Rows[index];

    public int Count => Rows.Count;

    public IEnumerable<TValue> this[TKey key] => Indexes[key].Select(f => Rows[f]);

    public bool Contains(TValue value)
    {
        return Rows.Contains(value);
    }

    public bool Contains(TValue value, Func<TValue, TValue, bool> comparer)
    {
        return Rows.Any(row => comparer(row, value));
    }

    public bool Contains(TKey key, TValue value)
    {
        if (Indexes.TryGetValue(key, out var values))
            foreach (var index in values)
                if (Rows[index].Equals(value))
                    break;
        return false;
    }

    public bool ContainsKey(TKey key)
    {
        return Indexes.ContainsKey(key);
    }

    public bool TryGetIndexedValues(TKey key, out IReadOnlyList<TValue> values)
    {
        if (Indexes.TryGetValue(key, out var matchedIndexes))
        {
            var resultValues = new List<TValue>();

            foreach (var rowIndex in matchedIndexes)
                resultValues.Add(Rows[rowIndex]);

            values = resultValues;
            return true;
        }

        values = new List<TValue>();
        return false;
    }

    protected void AddIndex(TKey index)
    {
        Indexes.Add(index, new List<int>());
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