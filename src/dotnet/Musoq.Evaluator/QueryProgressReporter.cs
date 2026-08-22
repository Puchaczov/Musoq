using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator;

internal sealed class QueryProgressReporter
{
    private readonly object _gate = new();
    private readonly QueryProgressEventHandler _handler;
    private readonly QueryProgressOptions _options;
    private readonly object _sender;
    private readonly string _queryId;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, long> _sourceRows = new(StringComparer.Ordinal);
    private readonly List<QueryProgressSourceReporter> _sources = [];
    private long _queryRows;
    private long _sequence;
    private int _completed;
    private int _completing;

    public QueryProgressReporter(
        string queryId,
        object sender,
        QueryProgressEventHandler handler,
        QueryProgressOptions options)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);

        _queryId = queryId;
        _sender = sender;
        _handler = handler;
        _options = options;
        _timeProvider = options.TimeProvider ?? throw new ArgumentException("A time provider is required.", nameof(options));
    }

    public QueryProgressSourceReporter CreateSource(string sourceContextId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContextId);
        var source = new QueryProgressSourceReporter(this, sourceContextId, _timeProvider.GetTimestamp());
        lock (_gate)
        {
            if (_completed == 0 && _completing == 0)
            {
                _sources.Add(source);
                return source;
            }
        }

        source.CloseWithoutFlush();
        return source;
    }

    public void Complete()
    {
        QueryProgressSourceReporter[] sources;
        lock (_gate)
        {
            if (_completed != 0 || _completing != 0)
                return;

            _completing = 1;
            sources = _sources.ToArray();
        }

        try
        {
            foreach (var source in sources)
                source.CloseAndFlush();
        }
        catch
        {
            lock (_gate)
                _completing = 0;

            throw;
        }

        lock (_gate)
        {
            if (_completed != 0)
                return;

            _completed = 1;
            _completing = 0;
            try
            {
                PublishLocked(sourceContextId: null, sourceRowsProcessed: null, isFinal: true);
            }
            catch
            {
                // Keep completion retryable when a consumer callback fails. The
                // counters and sequence remain monotonic across the retry.
                _completed = 0;
                throw;
            }
        }
    }

    private void Add(string sourceContextId, long rows, bool force)
    {
        if (rows <= 0 && !force)
            return;

        lock (_gate)
        {
            if (_completed != 0)
                return;

            if (rows > 0)
            {
                _queryRows = checked(_queryRows + rows);
                _sourceRows[sourceContextId] = _sourceRows.TryGetValue(sourceContextId, out var current)
                    ? checked(current + rows)
                    : rows;
            }

            if (force || rows > 0)
                PublishLocked(sourceContextId, rows > 0 ? _sourceRows[sourceContextId] : null, isFinal: false);
        }
    }

    private void PublishLocked(string? sourceContextId, long? sourceRowsProcessed, bool isFinal)
    {
        var sequence = checked(++_sequence);
        _handler(
            _sender,
            new QueryProgressEventArgs(
                _queryId,
                sourceContextId,
                _queryRows,
                sourceRowsProcessed,
                sequence,
                isFinal));
    }

    internal sealed class QueryProgressSourceReporter
    {
        private readonly QueryProgressReporter _owner;
        private readonly string _sourceContextId;
        private long _pending;
        private long _lastPublishedTimestamp;
        private int _activeWriters;
        private int _closed;

        internal QueryProgressSourceReporter(
            QueryProgressReporter owner,
            string sourceContextId,
            long initialTimestamp)
        {
            _owner = owner;
            _sourceContextId = sourceContextId;
            _lastPublishedTimestamp = initialTimestamp;
        }

        public void Add(long rows)
        {
            if (rows <= 0)
                return;

            if (Volatile.Read(ref _closed) != 0)
                return;

            var shouldFlush = false;
            var flushTimestamp = 0L;
            Interlocked.Increment(ref _activeWriters);
            try
            {
                // The second state check closes the small race between the first
                // check and completion. Completion waits for writers that passed
                // this check before it flushes the pending rows.
                if (Volatile.Read(ref _closed) != 0)
                    return;

                var pending = Interlocked.Add(ref _pending, rows);
                var now = _owner._timeProvider.GetTimestamp();
                var elapsed = _owner._timeProvider.GetElapsedTime(
                    Volatile.Read(ref _lastPublishedTimestamp),
                    now);
                if (pending >= _owner._options.RowsPerUpdate ||
                    elapsed >= _owner._options.MinimumInterval)
                {
                    // Flush happens after the writer leaves the completion
                    // barrier. The pending counter is atomic, so a concurrent
                    // flush can safely take ownership of this batch.
                    shouldFlush = true;
                    flushTimestamp = now;
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeWriters);
            }

            if (shouldFlush)
                FlushCore(flushTimestamp);
        }

        public void Flush()
        {
            WaitForActiveWriters();
            FlushCore(_owner._timeProvider.GetTimestamp());
        }

        public void CloseAndFlush()
        {
            Interlocked.Exchange(ref _closed, 1);
            WaitForActiveWriters();
            FlushCore(_owner._timeProvider.GetTimestamp());
        }

        private void FlushCore(long timestamp)
        {
            var pending = Interlocked.Exchange(ref _pending, 0);
            if (pending == 0)
                return;

            AdvanceLastPublishedTimestamp(timestamp);
            _owner.Add(_sourceContextId, pending, force: true);
        }

        private void AdvanceLastPublishedTimestamp(long timestamp)
        {
            while (true)
            {
                var previous = Volatile.Read(ref _lastPublishedTimestamp);
                if (timestamp <= previous ||
                    Interlocked.CompareExchange(ref _lastPublishedTimestamp, timestamp, previous) == previous)
                    return;
            }
        }

        internal void CloseWithoutFlush()
        {
            Interlocked.Exchange(ref _closed, 1);
        }

        private void WaitForActiveWriters()
        {
            var spinner = new SpinWait();
            while (Volatile.Read(ref _activeWriters) != 0)
                spinner.SpinOnce();
        }

    }
}

internal sealed class QueryProgressChunkEnumerable<T>(
    IEnumerable<IReadOnlyList<T>> source,
    QueryProgressReporter.QueryProgressSourceReporter reporter)
    : IEnumerable<IReadOnlyList<T>>
{
    private readonly IEnumerable<IReadOnlyList<T>> _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly QueryProgressReporter.QueryProgressSourceReporter _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));

    public IEnumerator<IReadOnlyList<T>> GetEnumerator()
    {
        try
        {
            return new Enumerator(_source.GetEnumerator(), _reporter);
        }
        catch
        {
            _reporter.Flush();
            throw;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(
        IEnumerator<IReadOnlyList<T>> inner,
        QueryProgressReporter.QueryProgressSourceReporter reporter)
        : IEnumerator<IReadOnlyList<T>>
    {
        private int _disposed;

        public IReadOnlyList<T> Current => inner.Current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            try
            {
                var moved = inner.MoveNext();
                if (moved)
                    reporter.Add(inner.Current?.Count ?? 0);
                else
                    reporter.Flush();

                return moved;
            }
            catch
            {
                reporter.Flush();
                throw;
            }
        }

        public void Reset() => inner.Reset();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                reporter.Flush();
            }
            finally
            {
                inner.Dispose();
            }
        }
    }
}
