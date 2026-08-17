using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Diagnostics;

public sealed class ProfiledEnumerator<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> _source;
    private readonly SourceProfileRecorder _recorder;
    private readonly IProfileClock _clock;
    private readonly bool _useAdaptiveTiming;
    private readonly OperatorProfileExclusionTarget _operatorExclusionTarget;
    private AdaptiveSourceTimingSampler _adaptiveTimingSampler;
    private bool _started;
    private bool _finished;
    private bool _hasPreviousMoveNextEndTimestamp;
    private long _previousMoveNextEndTimestamp;
    private bool _previousMoveNextWasTimed;
    private long _startedTimestamp;
    private long _rowsRead;
    private bool _hasFirstRow;
    private long _firstRowTimestamp;
    private long _lastRowTimestamp;
    private TimeSpan _moveNextWaitTime;
    private TimeSpan _consumerGapTime;
    private long _moveNextWaitTicksToExclude;
    private long _moveNextCalls;
    private long _timedMoveNextCalls;
    private long _untimedMoveNextCalls;
    private long _timedConsumerGapCount;
    private int _exceptionCount;
    private string? _exceptionType;
    private string? _exceptionMessage;
    private int _flushed;
    private int _disposed;

    public ProfiledEnumerator(IEnumerator<T> source, SourceProfileRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(recorder);

        _source = source;
        _recorder = recorder;
        _clock = recorder.Clock;
        _useAdaptiveTiming = recorder.UseAdaptiveTiming;
        _operatorExclusionTarget = recorder.CaptureCurrentOperatorExclusionTarget();
    }

    public T Current => _source.Current;

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_finished)
        {
            Flush();
            return false;
        }

        var nextAttempt = _moveNextCalls + 1;
        var shouldTimeMoveNext = ShouldTimeMoveNext(nextAttempt);
        var moveNextStartTimestamp = shouldTimeMoveNext ? _clock.GetTimestamp() : 0L;

        if (!_started)
        {
            _started = true;
            _startedTimestamp = moveNextStartTimestamp;
        }
        else if (shouldTimeMoveNext && _previousMoveNextWasTimed && _hasPreviousMoveNextEndTimestamp)
        {
            _consumerGapTime += _clock.GetElapsedTime(_previousMoveNextEndTimestamp, moveNextStartTimestamp);
            _timedConsumerGapCount++;
        }

        try
        {
            var hasNext = _source.MoveNext();
            _moveNextCalls++;

            if (shouldTimeMoveNext)
            {
                var moveNextEndTimestamp = _clock.GetTimestamp();
                RecordMoveNextWait(nextAttempt, moveNextStartTimestamp, moveNextEndTimestamp);
                _previousMoveNextEndTimestamp = moveNextEndTimestamp;
                _hasPreviousMoveNextEndTimestamp = true;
                _previousMoveNextWasTimed = true;

                if (hasNext)
                {
                    RecordRowRead(moveNextEndTimestamp);
                    return true;
                }
            }
            else
            {
                _untimedMoveNextCalls++;
                _previousMoveNextWasTimed = false;

                if (hasNext)
                {
                    RecordUntimedRowRead();
                    return true;
                }
            }

            _finished = true;
            Flush();
            return false;
        }
        catch (Exception exception)
        {
            _moveNextCalls++;

            if (shouldTimeMoveNext)
            {
                var moveNextEndTimestamp = _clock.GetTimestamp();
                RecordMoveNextWait(nextAttempt, moveNextStartTimestamp, moveNextEndTimestamp);
                _previousMoveNextEndTimestamp = moveNextEndTimestamp;
                _hasPreviousMoveNextEndTimestamp = true;
                _previousMoveNextWasTimed = true;
            }
            else
            {
                _untimedMoveNextCalls++;
                _previousMoveNextWasTimed = false;
            }

            RecordException(exception);
            _finished = true;
            Flush();

            throw;
        }
    }

    public void Reset() => _source.Reset();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Flush();
        _source.Dispose();
    }

    private bool ShouldTimeMoveNext(long nextAttempt) =>
        !_useAdaptiveTiming || _adaptiveTimingSampler.ShouldTime(nextAttempt);

    private void RecordMoveNextWait(long attempt, long startTimestamp, long endTimestamp)
    {
        var elapsed = _clock.GetElapsedTime(startTimestamp, endTimestamp);
        _moveNextWaitTime += elapsed;
        _timedMoveNextCalls++;
        if (_useAdaptiveTiming)
            _adaptiveTimingSampler.RecordTimedWait(attempt, elapsed);

        if (_operatorExclusionTarget.IsEnabled)
            _moveNextWaitTicksToExclude += elapsed.Ticks;
    }

    private void RecordRowRead(long rowTimestamp)
    {
        _rowsRead++;
        if (!_hasFirstRow)
        {
            _hasFirstRow = true;
            _firstRowTimestamp = rowTimestamp;
        }

        _lastRowTimestamp = rowTimestamp;
    }

    private void RecordUntimedRowRead()
    {
        _rowsRead++;
    }

    private void RecordException(Exception exception)
    {
        _exceptionCount++;
        _exceptionType = exception.GetType().FullName;
        _exceptionMessage = exception.Message;
    }

    private void Flush()
    {
        if (Interlocked.Exchange(ref _flushed, 1) != 0)
            return;

        var isTimingEstimated = _untimedMoveNextCalls > 0;

        _recorder.RecordEnumeration(new SourceEnumerationProfileBatch(
            _started,
            _startedTimestamp,
            _rowsRead,
            _hasFirstRow,
            _firstRowTimestamp,
            _lastRowTimestamp,
            EstimateMoveNextWait(isTimingEstimated),
            EstimateConsumerGap(isTimingEstimated),
            _exceptionCount,
            _exceptionType,
            _exceptionMessage,
            isTimingEstimated,
            _timedMoveNextCalls,
            _untimedMoveNextCalls));
        _operatorExclusionTarget.ExcludeElapsedTicks(_moveNextWaitTicksToExclude);
    }

    private TimeSpan EstimateMoveNextWait(bool isTimingEstimated)
    {
        if (!isTimingEstimated || _timedMoveNextCalls == 0)
            return _moveNextWaitTime;

        return ScaleTime(_moveNextWaitTime, _timedMoveNextCalls + _untimedMoveNextCalls, _timedMoveNextCalls);
    }

    private TimeSpan EstimateConsumerGap(bool isTimingEstimated)
    {
        if (!isTimingEstimated || _timedConsumerGapCount == 0)
            return _consumerGapTime;

        var totalGapCount = Math.Max(0, _timedMoveNextCalls + _untimedMoveNextCalls - 1);
        return ScaleTime(_consumerGapTime, totalGapCount, _timedConsumerGapCount);
    }

    private static TimeSpan ScaleTime(TimeSpan value, long totalCount, long sampledCount)
    {
        if (value <= TimeSpan.Zero || totalCount <= 0 || sampledCount <= 0)
            return value;

        var scaledTicks = checked((long)Math.Round(value.Ticks * ((double)totalCount / sampledCount)));
        return scaledTicks > 0 ? TimeSpan.FromTicks(scaledTicks) : TimeSpan.Zero;
    }
}
