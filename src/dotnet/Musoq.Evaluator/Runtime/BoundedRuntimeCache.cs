using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Runtime;

internal sealed class BoundedRuntimeCache<TKey, TValue>
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly Dictionary<TKey, TValue> _values;
    private readonly Queue<TKey> _insertionOrder = new();
    private readonly int _maxSize;
    private Dictionary<TKey, TValue> _readSnapshot;

    public BoundedRuntimeCache(int maxSize, IEqualityComparer<TKey>? comparer = null)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Cache size must be positive.");

        _maxSize = maxSize;
        _values = new Dictionary<TKey, TValue>(comparer);
        _readSnapshot = new Dictionary<TKey, TValue>(_values, _values.Comparer);
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

        lock (_gate)
        {
            if (_values.TryGetValue(key, out var existing) && isCurrent(existing))
                return existing;

            var value = factory(key);
            if (_values.ContainsKey(key))
            {
                _values[key] = value;
            }
            else
            {
                EvictOneIfFull();
                _values.Add(key, value);
                _insertionOrder.Enqueue(key);
            }

            PublishReadSnapshot();
            return value;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        var snapshot = Volatile.Read(ref _readSnapshot);
        return snapshot.TryGetValue(key, out value!);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _values.Clear();
            _insertionOrder.Clear();
            PublishReadSnapshot();
        }
    }

    private void PublishReadSnapshot()
    {
        Volatile.Write(
            ref _readSnapshot,
            new Dictionary<TKey, TValue>(_values, _values.Comparer));
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
