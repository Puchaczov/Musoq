using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.DataSources;

public sealed class DiagnosticChunkWriter<T> : IChunkWriter<T>
{
    private readonly BlockingCollection<IReadOnlyList<T>> _chunks;
    private readonly IChunkPipelineMetrics _metrics;

    internal DiagnosticChunkWriter(
        BlockingCollection<IReadOnlyList<T>> chunks,
        DiagnosticChunkMetrics metrics,
        CancellationToken token)
    {
        _chunks = chunks;
        _metrics = metrics;
        CancellationToken = token;
    }

    public CancellationToken CancellationToken { get; }

    public void Write(IReadOnlyList<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        CancellationToken.ThrowIfCancellationRequested();

        foreach (var chunk in RowChunking.NormalizeSourceChunk(rows))
        {
            if (TryAddWithoutWaiting(chunk))
            {
                _metrics.RecordProduced(chunk.Count, _chunks.Count);
                continue;
            }

            _metrics.RecordProducerWaitOnFull();

            var startedTimestamp = Stopwatch.GetTimestamp();
            using (_metrics.MeasureProducerWaitOnFull())
            {
                AddWithCancellation(chunk);
            }

            _metrics.RecordProducerWaitOnFullElapsed(Stopwatch.GetElapsedTime(startedTimestamp));
            _metrics.RecordProduced(chunk.Count, _chunks.Count);
        }
    }

    private bool TryAddWithoutWaiting(IReadOnlyList<T> rows)
    {
        try
        {
            return _chunks.TryAdd(rows);
        }
        catch (InvalidOperationException) when (CancellationToken.IsCancellationRequested || _chunks.IsAddingCompleted)
        {
            throw new OperationCanceledException(CancellationToken);
        }
    }

    private void AddWithCancellation(IReadOnlyList<T> rows)
    {
        try
        {
            _chunks.Add(rows, CancellationToken);
        }
        catch (InvalidOperationException) when (CancellationToken.IsCancellationRequested || _chunks.IsAddingCompleted)
        {
            throw new OperationCanceledException(CancellationToken);
        }
    }
}
