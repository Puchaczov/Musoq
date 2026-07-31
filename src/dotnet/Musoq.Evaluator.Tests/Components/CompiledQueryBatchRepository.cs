using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests.Components;

internal sealed class CompiledQueryBatchRepository<TKey> : IDisposable
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly Lazy<IReadOnlyDictionary<TKey, CompiledQueryBatchEntry>> _factory;
    private Dictionary<TKey, CompiledQueryBatchEntry>? _remaining;
    private HashSet<TKey>? _knownKeys;
    private bool _disposed;

    public CompiledQueryBatchRepository(Func<IReadOnlyDictionary<TKey, CompiledQuery>> factory)
        : this(() => factory()
            .ToDictionary(
                static entry => entry.Key,
                static entry => CompiledQueryBatchEntry.Success(entry.Value)))
    {
    }

    internal CompiledQueryBatchRepository(
        Func<IReadOnlyDictionary<TKey, CompiledQueryBatchEntry>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = new Lazy<IReadOnlyDictionary<TKey, CompiledQueryBatchEntry>>(
            factory,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal bool IsValueCreated => _factory.IsValueCreated;

    public CompiledQuery Take(TKey key)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            EnsureInitialized();

            if (_remaining!.Remove(key, out var entry))
            {
                if (entry.Query is { } query)
                    return query;

                throw new InvalidOperationException(
                    $"Compiled query batch key '{key}' failed to compile.",
                    entry.Exception);
            }

            if (_knownKeys!.Contains(key))
                throw new InvalidOperationException($"Compiled query batch key '{key}' was already consumed.");

            throw new KeyNotFoundException($"Compiled query batch did not produce key '{key}'.");
        }
    }

    public void Dispose()
    {
        CompiledQuery[] unconsumed;
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            unconsumed = _remaining?.Values
                .Where(static entry => entry.Query is not null)
                .Select(static entry => entry.Query!)
                .ToArray() ?? [];
            _remaining?.Clear();
        }

        foreach (var query in unconsumed)
            query.Dispose();
    }

    private void EnsureInitialized()
    {
        if (_remaining is not null)
            return;

        var queries = _factory.Value ??
                      throw new InvalidOperationException("Compiled query batch factory returned null.");
        _remaining = new Dictionary<TKey, CompiledQueryBatchEntry>(queries);
        _knownKeys = new HashSet<TKey>(_remaining.Keys);
    }
}

internal sealed record CompiledQueryBatchEntry(
    CompiledQuery? Query,
    Exception? Exception)
{
    internal static CompiledQueryBatchEntry Success(CompiledQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new CompiledQueryBatchEntry(query, null);
    }

    internal static CompiledQueryBatchEntry Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new CompiledQueryBatchEntry(null, exception);
    }
}
