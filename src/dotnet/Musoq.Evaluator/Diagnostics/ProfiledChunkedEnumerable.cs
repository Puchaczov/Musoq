using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Diagnostics;

public sealed class ProfiledChunkedEnumerable<T> : IEnumerable<IReadOnlyList<T>>
{
    private readonly IEnumerable<IReadOnlyList<T>> _source;
    private readonly SourceProfileRecorder _recorder;

    private ProfiledChunkedEnumerable(IEnumerable<IReadOnlyList<T>> source, SourceProfileRecorder recorder)
    {
        _source = source;
        _recorder = recorder;
    }

    public static IEnumerable<IReadOnlyList<T>> Create(
        IEnumerable<IReadOnlyList<T>> source,
        SourceProfileRecorder recorder)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(recorder);

        return new ProfiledChunkedEnumerable<T>(source, recorder);
    }

    public IEnumerator<IReadOnlyList<T>> GetEnumerator()
    {
        return new Enumerator(_source.GetEnumerator(), _recorder);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private sealed class Enumerator : IEnumerator<IReadOnlyList<T>>
    {
        private readonly IEnumerator<IReadOnlyList<T>> _source;
        private readonly SourceProfileRecorder _recorder;
        private readonly IProfileClock _clock;
        private readonly OperatorProfileExclusionTarget _operatorExclusionTarget;
        private bool _started;
        private bool _finished;
        private long _startedTimestamp;
        private long _rowsRead;
        private bool _hasFirstRow;
        private long _firstRowTimestamp;
        private long _lastRowTimestamp;
        private TimeSpan _moveNextWaitTime;
        private long _moveNextWaitTicksToExclude;
        private long _moveNextCalls;
        private int _exceptionCount;
        private string? _exceptionType;
        private string? _exceptionMessage;
        private int _flushed;
        private int _disposed;

        public Enumerator(IEnumerator<IReadOnlyList<T>> source, SourceProfileRecorder recorder)
        {
            _source = source;
            _recorder = recorder;
            _clock = recorder.Clock;
            _operatorExclusionTarget = recorder.CaptureCurrentOperatorExclusionTarget();
        }

        public IReadOnlyList<T> Current => _source.Current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_finished)
            {
                Flush();
                return false;
            }

            var moveNextStartTimestamp = _clock.GetTimestamp();
            if (!_started)
            {
                _started = true;
                _startedTimestamp = moveNextStartTimestamp;
            }

            try
            {
                var hasNext = _source.MoveNext();
                var moveNextEndTimestamp = _clock.GetTimestamp();
                RecordMoveNextWait(moveNextStartTimestamp, moveNextEndTimestamp);
                _moveNextCalls++;

                if (!hasNext)
                {
                    _finished = true;
                    Flush();
                    return false;
                }

                RecordRowsRead(_source.Current.Count, moveNextEndTimestamp);
                return true;
            }
            catch (Exception exception)
            {
                var moveNextEndTimestamp = _clock.GetTimestamp();
                RecordMoveNextWait(moveNextStartTimestamp, moveNextEndTimestamp);
                _moveNextCalls++;
                RecordException(exception);
                _finished = true;
                Flush();
                throw;
            }
        }

        public void Reset()
        {
            _source.Reset();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            Flush();
            _source.Dispose();
        }

        private void RecordMoveNextWait(long startTimestamp, long endTimestamp)
        {
            var elapsed = _clock.GetElapsedTime(startTimestamp, endTimestamp);
            _moveNextWaitTime += elapsed;
            if (_operatorExclusionTarget.IsEnabled)
                _moveNextWaitTicksToExclude += elapsed.Ticks;
        }

        private void RecordRowsRead(int count, long timestamp)
        {
            if (count <= 0)
                return;

            _rowsRead += count;
            if (!_hasFirstRow)
            {
                _hasFirstRow = true;
                _firstRowTimestamp = timestamp;
            }

            _lastRowTimestamp = timestamp;
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

            _recorder.RecordEnumeration(new SourceEnumerationProfileBatch(
                _started,
                _startedTimestamp,
                _rowsRead,
                _hasFirstRow,
                _firstRowTimestamp,
                _lastRowTimestamp,
                _moveNextWaitTime,
                TimeSpan.Zero,
                _exceptionCount,
                _exceptionType,
                _exceptionMessage,
                false,
                _moveNextCalls,
                0));
            _operatorExclusionTarget.ExcludeElapsedTicks(_moveNextWaitTicksToExclude);
        }
    }
}
