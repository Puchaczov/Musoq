using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Musoq.Evaluator.Tests.Schema.RecursiveCte;

public sealed class RecursiveGraphSourceRecorder
{
    private readonly ConcurrentDictionary<string, int> _created = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _enumerated = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _disposed = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _rows = new(StringComparer.OrdinalIgnoreCase);
    private int _neighborInvocations;

    public int NeighborInvocations => Volatile.Read(ref _neighborInvocations);

    public int Created(string sourceName) => Read(_created, sourceName);

    public int Enumerated(string sourceName) => Read(_enumerated, sourceName);

    public int Disposed(string sourceName) => Read(_disposed, sourceName);

    public int RowsYielded(string sourceName) => Read(_rows, sourceName);

    internal void SourceCreated(string sourceName) => _created.AddOrUpdate(sourceName, 1, static (_, count) => count + 1);

    internal void EnumerationStarted(string sourceName) =>
        _enumerated.AddOrUpdate(sourceName, 1, static (_, count) => count + 1);

    internal void EnumerationDisposed(string sourceName) =>
        _disposed.AddOrUpdate(sourceName, 1, static (_, count) => count + 1);

    internal void RowsProduced(string sourceName, int count) =>
        _rows.AddOrUpdate(sourceName, count, (_, current) => current + count);

    internal void NeighborInvoked() => Interlocked.Increment(ref _neighborInvocations);

    private static int Read(ConcurrentDictionary<string, int> values, string sourceName) =>
        values.TryGetValue(sourceName, out var count) ? count : 0;
}
