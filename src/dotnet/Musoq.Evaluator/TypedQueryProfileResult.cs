using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Musoq.Evaluator.Diagnostics;

namespace Musoq.Evaluator;

public sealed class TypedQueryProfileResult<TOut>
{
    private readonly QueryProfileSnapshot _profile;
    private readonly string _profileText;
    private int _isFinalized;

    public TypedQueryProfileResult(
        IEnumerable<TOut> rows,
        QueryProfileSnapshot profile,
        string profileText,
        bool isSourceExecutionComplete = false)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _profileText = profileText ?? throw new ArgumentNullException(nameof(profileText));
        IsSourceExecutionComplete = isSourceExecutionComplete;
        Rows = new FinalizingEnumerable(rows, this);
    }

    public IEnumerable<TOut> Rows { get; }

    public bool IsFinalized => _isFinalized != 0;

    public bool IsSourceExecutionComplete { get; }

    public bool IsComplete { get; private set; }

    public Exception? Exception { get; private set; }

    public QueryProfileSnapshot Profile
    {
        get
        {
            ThrowIfNotFinalized();
            return _profile;
        }
    }

    public string ProfileText
    {
        get
        {
            ThrowIfNotFinalized();
            return _profileText;
        }
    }

    private void Finalize(bool isComplete, Exception? exception)
    {
        if (Interlocked.Exchange(ref _isFinalized, 1) != 0)
            return;

        IsComplete = isComplete;
        Exception = exception;
    }

    private void ThrowIfNotFinalized()
    {
        if (!IsFinalized)
            throw new InvalidOperationException("Typed query profile is available only after row enumeration completes, is disposed, or fails.");
    }

    private sealed class FinalizingEnumerable(
        IEnumerable<TOut> source,
        TypedQueryProfileResult<TOut> owner) : IEnumerable<TOut>
    {
        private int _started;

        public IEnumerator<TOut> GetEnumerator()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                throw new InvalidOperationException("Typed profile result rows can be enumerated only once.");

            return new FinalizingEnumerator(source.GetEnumerator(), owner);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class FinalizingEnumerator(
        IEnumerator<TOut> source,
        TypedQueryProfileResult<TOut> owner) : IEnumerator<TOut>
    {
        private bool _completed;
        private int _disposed;

        public TOut Current => source.Current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            try
            {
                var moved = source.MoveNext();
                if (!moved)
                {
                    _completed = true;
                    owner.Finalize(isComplete: true, exception: null);
                }

                return moved;
            }
            catch (Exception ex)
            {
                owner.Finalize(isComplete: false, ex);
                throw;
            }
        }

        public void Reset()
        {
            source.Reset();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            try
            {
                source.Dispose();
            }
            finally
            {
                if (!_completed && !owner.IsFinalized)
                    owner.Finalize(isComplete: false, exception: null);
            }
        }
    }
}
