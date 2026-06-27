using System;
using System.Threading;

namespace Musoq.Evaluator.Diagnostics;

internal sealed class OperatorProfileRecorder
{
    private readonly object _gate = new();
    private readonly IProfileClock _clock;
    private readonly string _id;
    private string _name;
    private long _inputRows;
    private long _outputRows;
    private long _elapsedTicks;
    private int _exceptionCount;
    private string? _exceptionType;
    private string? _exceptionMessage;
    private int _hasActualStats;
    private int _hasElapsedTime;

    public OperatorProfileRecorder(string id, string name, IProfileClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(clock);

        _id = id;
        _name = name;
        _clock = clock;
    }

    public OperatorProfileScope Begin(string name, QueryProfileRecorder owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(owner);

        lock (_gate)
            _name = name;

        MarkActualStats();
        return owner.BeginOperatorScope(this, _clock.GetTimestamp());
    }

    public OperatorProfileScope Begin(QueryProfileRecorder owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        MarkActualStats();

        return owner.BeginOperatorScope(this, _clock.GetTimestamp());
    }

    public OperatorProfileValueScope BeginValue(QueryProfileRecorder owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        MarkActualStats();

        return owner.BeginOperatorValueScope(this, _clock.GetTimestamp());
    }

    public void AddInputRows(long count)
    {
        if (count <= 0)
            return;

        MarkActualStats();
        Interlocked.Add(ref _inputRows, count);
    }

    public void AddOutputRows(long count)
    {
        if (count <= 0)
            return;

        MarkActualStats();
        Interlocked.Add(ref _outputRows, count);
    }

    public void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_gate)
        {
            MarkActualStats();
            _exceptionCount++;
            _exceptionType = exception.GetType().FullName;
            _exceptionMessage = exception.Message;
        }
    }

    public OperatorProfileSnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            return new OperatorProfileSnapshot(
                _id,
                _name,
                Interlocked.Read(ref _inputRows),
                Interlocked.Read(ref _outputRows),
                TimeSpan.FromTicks(Interlocked.Read(ref _elapsedTicks)),
                Volatile.Read(ref _hasActualStats) != 0,
                _exceptionCount,
                _exceptionType,
                _exceptionMessage)
            {
                HasElapsedTime = Volatile.Read(ref _hasElapsedTime) != 0
            };
        }
    }

    internal void Complete(long startedTimestamp, long excludedTicks, long inputRows, long outputRows)
    {
        var elapsed = _clock.GetElapsedTime(startedTimestamp, _clock.GetTimestamp());
        if (excludedTicks > 0)
        {
            var elapsedTicks = elapsed.Ticks - excludedTicks;
            elapsed = elapsedTicks > 0
                ? TimeSpan.FromTicks(elapsedTicks)
                : TimeSpan.Zero;
        }

        MarkActualStats();
        Volatile.Write(ref _hasElapsedTime, 1);
        if (elapsed > TimeSpan.Zero)
            Interlocked.Add(ref _elapsedTicks, elapsed.Ticks);
        if (inputRows > 0)
            Interlocked.Add(ref _inputRows, inputRows);
        if (outputRows > 0)
            Interlocked.Add(ref _outputRows, outputRows);
    }

    private void MarkActualStats()
    {
        Volatile.Write(ref _hasActualStats, 1);
    }
}
