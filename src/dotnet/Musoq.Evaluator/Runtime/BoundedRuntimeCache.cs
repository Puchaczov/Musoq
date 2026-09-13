using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Musoq.Evaluator.Runtime;

internal sealed class BoundedRuntimeCache<TKey, TValue>
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<TKey, TValue> _values;
    private readonly Queue<TKey> _insertionOrder = new();
    private readonly int _maxSize;

    public BoundedRuntimeCache(int maxSize, IEqualityComparer<TKey>? comparer = null)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Cache size must be positive.");

        _maxSize = maxSize;
        _values = new ConcurrentDictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
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

        return GetOrAdd(key, factory, static _ => true);
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory, Func<TValue, bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(isCurrent);

        if (_values.TryGetValue(key, out var existing) && isCurrent(existing))
            return existing;

        lock (_gate)
        {
            if (_values.TryGetValue(key, out existing) && isCurrent(existing))
                return existing;

            var value = factory(key);
            if (_values.ContainsKey(key))
            {
                _values[key] = value;
            }
            else
            {
                EvictOneIfFull();
                if (!_values.TryAdd(key, value))
                    throw new InvalidOperationException("A cache key was added outside the serialized mutation path.");
                _insertionOrder.Enqueue(key);
            }

            return value;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
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
            if (_values.TryRemove(oldestKey, out _))
                return;
        }
    }
}
