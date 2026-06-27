using System;
using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.DataSources;

public abstract class DiagnosticChunkedRowSource<T> : RowSource<T>
{
    private readonly SourceExecutionContext _context;
    private readonly string _sourceName;
    private readonly DiagnosticChunkedRowSourceOptions _options;

    protected DiagnosticChunkedRowSource(
        SourceExecutionContext context,
        string sourceName,
        DiagnosticChunkedRowSourceOptions? options = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        _sourceName = sourceName;
        _options = options ?? new DiagnosticChunkedRowSourceOptions();

        if (_options.CapacityInChunks <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk capacity must be greater than zero.");
    }

    public override IEnumerable<IReadOnlyList<T>> Chunks =>
        CreateChunks();

    protected abstract void CollectChunks(DiagnosticChunkWriter<T> writer);

    private IEnumerable<IReadOnlyList<T>> CreateChunks()
    {
        var metrics = new DiagnosticChunkMetrics(_context.Diagnostics, _sourceName);

        return new ProducerChunkedEnumerable<T, DiagnosticChunkWriter<T>>(
            _options.CapacityInChunks,
            _ => CancellationTokenSource.CreateLinkedTokenSource(_context.EndWorkToken),
            (chunks, token) => new DiagnosticChunkWriter<T>(chunks, metrics, token),
            CollectChunks,
            () => _context.EndWorkToken.IsCancellationRequested,
            metrics);
    }
}
