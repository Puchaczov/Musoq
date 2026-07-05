using System.Collections.Generic;

namespace Musoq.Evaluator.Runtime;

internal sealed class BoundedRuntimeCache<TKey, TValue>
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TKey, TValue> _values;
    private readonly Queue<TKey> _insertionOrder = new();
    private readonly int _maxSize;

    public BoundedRuntimeCache(int maxSize, IEqualityComparer<TKey>? comparer = null)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Cache size must be positive.");

        _maxSize = maxSize;
        _values = new Dictionary<TKey, TValue>(comparer);
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _values.Count;
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        lock (_gate)
        {
            if (_values.TryGetValue(key, out var existing))
                return existing;

            var value = factory(key);
            EvictOneIfFull();
            _values.Add(key, value);
            _insertionOrder.Enqueue(key);
            return value;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_gate)
            return _values.TryGetValue(key, out value!);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _values.Clear();
            _insertionOrder.Clear();
        }
    }

    private void EvictOneIfFull()
    {
        if (_values.Count < _maxSize)
            return;

        while (_insertionOrder.Count > 0)
        {
            var oldestKey = _insertionOrder.Dequeue();
            if (_values.Remove(oldestKey))
                return;
        }
    }
}
