using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Runtime;

/// <summary>
/// Bounded, thread-safe cache whose keys do not keep collectible type identities alive.
/// </summary>
internal sealed class WeakTypeRuntimeCache<TValue>
    where TValue : notnull
{
    private readonly object _gate = new();
    private readonly Queue<WeakReference<Type>> _insertionOrder = new();
    private readonly int _maxSize;
    private ConditionalWeakTable<Type, Entry> _values = new();
    private int _entryCount;

    public WeakTypeRuntimeCache(int maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Cache size must be positive.");

        _maxSize = maxSize;
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                PruneCollectedKeys();
                return _entryCount;
            }
        }
    }

    public TValue GetOrAdd(Type key, Func<Type, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        lock (_gate)
        {
            PruneCollectedKeys();
            if (_values.TryGetValue(key, out var existing))
                return existing.Value;

            var value = factory(key);
            EvictOneIfFull();
            _values.Add(key, new Entry(value));
            _insertionOrder.Enqueue(new WeakReference<Type>(key));
            _entryCount++;
            return value;
        }
    }

    public bool TryGetValue(Type key, out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            if (_values.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _values = new ConditionalWeakTable<Type, Entry>();
            _insertionOrder.Clear();
            _entryCount = 0;
        }
    }

    private void EvictOneIfFull()
    {
        if (_entryCount < _maxSize)
            return;

        while (_insertionOrder.Count > 0)
        {
            var oldestKey = _insertionOrder.Dequeue();
            if (!oldestKey.TryGetTarget(out var key))
            {
                _entryCount--;
                continue;
            }

            if (_values.Remove(key))
            {
                _entryCount--;
                return;
            }
        }
    }

    private void PruneCollectedKeys()
    {
        var queuedEntries = _insertionOrder.Count;
        for (var index = 0; index < queuedEntries; index++)
        {
            var keyReference = _insertionOrder.Dequeue();
            if (keyReference.TryGetTarget(out _))
            {
                _insertionOrder.Enqueue(keyReference);
            }
            else
            {
                _entryCount--;
            }
        }
    }

    private sealed class Entry(TValue value)
    {
        public TValue Value { get; } = value;
    }
}
