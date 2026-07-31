using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests;

internal sealed class GeneratedCodeArtifactCache<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _entries = new();

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, out bool cacheHit)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        var lazy = _entries.GetOrAdd(
            key,
            static (entryKey, factory) => new Lazy<TValue>(
                () => factory(entryKey),
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
            valueFactory);
        var wasCreated = lazy.IsValueCreated;

        try
        {
            var value = lazy.Value;
            cacheHit = wasCreated;
            return value;
        }
        catch
        {
            ((ICollection<KeyValuePair<TKey, Lazy<TValue>>>)_entries).Remove(
                new KeyValuePair<TKey, Lazy<TValue>>(key, lazy));
            throw;
        }
    }
}
