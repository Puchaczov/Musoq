using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Musoq.Schema.DataSources;

public class ChunkEnumerator<T> : IEnumerator<IReadOnlyList<T>>
{
    private readonly BlockingCollection<IReadOnlyList<T>> _readRows;
    private readonly CancellationToken _token;
    private readonly Action? _disposeResources;
    private readonly Action? _throwIfProducerFailed;
    private readonly IChunkPipelineMetrics? _metrics;
    private IReadOnlyList<T>? _currentChunk;
    private bool _completed;
    private int _disposed;

    public ChunkEnumerator(
        BlockingCollection<IReadOnlyList<T>> readRows,
        CancellationToken token,
        Action? disposeResources = null,
        Action? throwIfProducerFailed = null)
        : this(readRows, token, disposeResources, throwIfProducerFailed, null)
    {
    }

    internal ChunkEnumerator(
        BlockingCollection<IReadOnlyList<T>> readRows,
        CancellationToken token,
        Action? disposeResources,
        Action? throwIfProducerFailed,
        IChunkPipelineMetrics? metrics)
    {
        _readRows = readRows;
        _token = token;
        _disposeResources = disposeResources;
        _throwIfProducerFailed = throwIfProducerFailed;
        _metrics = metrics;
    }

    public bool MoveNext()
    {
        try
        {
            while (true)
            {
                if (_readRows.IsCompleted)
                {
                    _completed = true;
                    _currentChunk = null;
                    _throwIfProducerFailed?.Invoke();
                    return false;
                }

                if (!TryTakeNextChunk(out var currentChunk))
                    return Complete();

                if (currentChunk is not { Count: > 0 })
                    continue;

                _currentChunk = currentChunk;
                _metrics?.RecordChunkConsumed(currentChunk.Count, _readRows.Count);
                return true;
            }
        }
        catch (OperationCanceledException)
        {
            _completed = true;
            _currentChunk = null;
            throw;
        }
        catch (InvalidOperationException)
        {
            return Complete();
        }
    }

    public void Reset()
    {
        throw new NotSupportedException("Chunk enumerator does not support reset.");
    }

    public IReadOnlyList<T> Current =>
        _currentChunk != null
            ? _currentChunk
            : throw new InvalidOperationException("Enumeration has not started or has already finished.");

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (!_completed)
            _completed = true;

        _currentChunk = null;
        _disposeResources?.Invoke();
    }

    private bool TryTakeNextChunk(out IReadOnlyList<T> chunk)
    {
        if (_token.IsCancellationRequested)
            _token.ThrowIfCancellationRequested();

        if (_readRows.TryTake(out chunk!))
            return true;

        if (_readRows.IsCompleted)
            return false;

        _metrics?.RecordConsumerWaitOnEmpty();
        var startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            if (_metrics == null)
            {
                chunk = _token.CanBeCanceled ? _readRows.Take(_token) : _readRows.Take();
            }
            else
            {
                using (_metrics.MeasureConsumerWaitOnEmpty())
                {
                    chunk = _token.CanBeCanceled ? _readRows.Take(_token) : _readRows.Take();
                }
            }
        }
        finally
        {
            _metrics?.RecordConsumerWaitOnEmptyElapsed(Stopwatch.GetElapsedTime(startedTimestamp));
        }

        return true;
    }

    private bool Complete()
    {
        _completed = true;
        _currentChunk = null;
        _throwIfProducerFailed?.Invoke();
        return false;
    }
}
