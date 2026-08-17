using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Diagnostics;

public sealed class SourceProfileRecorder
{
    private readonly object _gate = new();
    private readonly IProfileClock _clock;
    private readonly QueryProfileRecorder? _queryRecorder;
    private readonly string _name;
    private bool _hasStarted;
    private long _startedTimestamp;
    private long _rowsRead;
    private TimeSpan? _firstRowLatency;
    private TimeSpan? _lastRowTime;
    private TimeSpan _moveNextWaitTime;
    private TimeSpan _consumerGapTime;
    private int _exceptionCount;
    private string? _exceptionType;
    private string? _exceptionMessage;
    private SourceProfileTimingMode _timingMode;
    private bool _isTimingEstimated;
    private long _timedMoveNextCalls;
    private long _untimedMoveNextCalls;
    private long _rowsProduced;
    private long _bytesRead;
    private readonly Dictionary<string, long> _metrics = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Name, SourceDiagnosticOperation Operation), OperationAccumulator> _operations = [];

    public SourceProfileRecorder(string name, IProfileClock clock)
        : this(name, clock, null)
    {
    }

    internal SourceProfileRecorder(string name, IProfileClock clock, QueryProfileRecorder? queryRecorder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clock);

        _name = name;
        _clock = clock;
        _queryRecorder = queryRecorder;
    }

    public string Name => _name;

    public IProfileClock Clock => _clock;

    internal bool UseAdaptiveTiming => _timingMode == SourceProfileTimingMode.Adaptive;

    public SourceDiagnostics CreateDiagnostics() => new(new SourceProfileDiagnosticsSink(this));

    internal void EnableAdaptiveTiming()
    {
        _timingMode = SourceProfileTimingMode.Adaptive;
    }

    internal void ExcludeCurrentOperatorElapsed(long startTimestamp, long endTimestamp)
    {
        _queryRecorder?.ExcludeCurrentOperatorElapsed(startTimestamp, endTimestamp);
    }

    internal OperatorProfileExclusionTarget CaptureCurrentOperatorExclusionTarget()
    {
        return _queryRecorder?.CaptureCurrentOperatorExclusionTarget() ?? default;
    }

    internal void RecordEnumeration(SourceEnumerationProfileBatch batch)
    {
        lock (_gate)
        {
            if (batch.HasStarted && !_hasStarted)
            {
                _hasStarted = true;
                _startedTimestamp = batch.StartedTimestamp;
            }

            _rowsRead += batch.RowsRead;
            _moveNextWaitTime += batch.MoveNextWaitTime;
            _consumerGapTime += batch.ConsumerGapTime;
            _isTimingEstimated |= batch.IsTimingEstimated;
            _timedMoveNextCalls += batch.TimedMoveNextCalls;
            _untimedMoveNextCalls += batch.UntimedMoveNextCalls;

            if (batch.HasFirstRow)
            {
                var firstRowLatency = _hasStarted
                    ? _clock.GetElapsedTime(_startedTimestamp, batch.FirstRowTimestamp)
                    : TimeSpan.Zero;
                var lastRowTime = _hasStarted
                    ? _clock.GetElapsedTime(_startedTimestamp, batch.LastRowTimestamp)
                    : TimeSpan.Zero;

                _firstRowLatency ??= firstRowLatency;
                _lastRowTime = lastRowTime;
            }

            if (batch.ExceptionCount > 0)
            {
                _exceptionCount += batch.ExceptionCount;
                _exceptionType = batch.ExceptionType;
                _exceptionMessage = batch.ExceptionMessage;
            }
        }
    }

    internal void AddRowsProduced(long count)
    {
        if (count <= 0)
            return;

        lock (_gate)
        {
            _rowsProduced += count;
        }
    }

    internal void AddBytesRead(long bytes)
    {
        if (bytes <= 0)
            return;

        lock (_gate)
        {
            _bytesRead += bytes;
        }
    }

    internal void AddMetric(string name, long value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_gate)
        {
            _metrics.TryGetValue(name, out var current);
            _metrics[name] = current + value;
        }
    }

    internal void RecordOperation(string name, SourceDiagnosticOperation operation, long startTimestamp, long endTimestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var elapsed = _clock.GetElapsedTime(startTimestamp, endTimestamp);

        lock (_gate)
        {
            var key = (name, operation);
            _operations.TryGetValue(key, out var current);
            _operations[key] = new OperationAccumulator(current.Count + 1, current.ElapsedTime + elapsed);
        }
    }

    public SourceProfileSnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            var metrics = new Dictionary<string, long>(_metrics, StringComparer.Ordinal);
            var diagnosis = SourceProfileDiagnosisClassifier.Classify(
                _rowsRead,
                _moveNextWaitTime,
                _consumerGapTime,
                _exceptionCount,
                metrics);

            return new SourceProfileSnapshot(
                _name,
                _rowsRead,
                _firstRowLatency,
                _lastRowTime,
                _moveNextWaitTime,
                _consumerGapTime,
                _exceptionCount,
                _exceptionType,
                _exceptionMessage,
                diagnosis)
            {
                RowsProduced = _rowsProduced,
                BytesRead = _bytesRead,
                IsTimingEstimated = _isTimingEstimated,
                TimedMoveNextCalls = _timedMoveNextCalls,
                UntimedMoveNextCalls = _untimedMoveNextCalls,
                Metrics = metrics,
                Operations = _operations
                    .OrderBy(static entry => entry.Key.Name, StringComparer.Ordinal)
                    .ThenBy(static entry => entry.Key.Operation)
                    .Select(static entry => new SourceOperationProfileSnapshot(
                        entry.Key.Name,
                        entry.Key.Operation,
                        entry.Value.Count,
                        entry.Value.ElapsedTime))
                    .ToArray()
            };
        }
    }

    private readonly record struct OperationAccumulator(long Count, TimeSpan ElapsedTime);

    private sealed class SourceProfileDiagnosticsSink(SourceProfileRecorder recorder) : ISourceDiagnosticsSink
    {
        public IDisposable Measure(string name, SourceDiagnosticOperation operation)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return new OperationScope(recorder, name, operation, recorder.Clock.GetTimestamp());
        }

        public void AddRowsProduced(long count) => recorder.AddRowsProduced(count);

        public void AddBytesRead(long bytes) => recorder.AddBytesRead(bytes);

        public void AddMetric(string name, long value) => recorder.AddMetric(name, value);
    }

    private sealed class OperationScope(
        SourceProfileRecorder recorder,
        string name,
        SourceDiagnosticOperation operation,
        long startedTimestamp) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            recorder.RecordOperation(name, operation, startedTimestamp, recorder.Clock.GetTimestamp());
        }
    }
}
