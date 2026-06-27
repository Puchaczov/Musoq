using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Runtime;

internal sealed class QueryRowsScope<T> : IDisposable
{
    private readonly Action? _onCompleted;
    private readonly Action<Exception>? _onException;
    private readonly Action? _onDisposed;
    private int _state;

    public QueryRowsScope(
        IEnumerable<T> rows,
        CancellationTokenSource cancellation,
        Action? onCompleted,
        Action<Exception>? onException,
        Action? onDisposed)
    {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        Cancellation = cancellation ?? throw new ArgumentNullException(nameof(cancellation));
        _onCompleted = onCompleted;
        _onException = onException;
        _onDisposed = onDisposed;
    }

    public IEnumerable<T> Rows { get; }

    public CancellationTokenSource Cancellation { get; }

    public void Complete()
    {
        if (Interlocked.Exchange(ref _state, 1) != 0)
            return;

        try
        {
            _onCompleted?.Invoke();
        }
        finally
        {
            Cancellation.Dispose();
        }
    }

    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (Interlocked.Exchange(ref _state, 1) != 0)
            return;

        try
        {
            _onException?.Invoke(exception);
        }
        finally
        {
            Cancellation.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _state, 1) != 0)
            return;

        try
        {
            Cancellation.Cancel();
            _onDisposed?.Invoke();
        }
        finally
        {
            Cancellation.Dispose();
        }
    }

    public void DisposeEnumerator(IDisposable enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);
        if (Interlocked.Exchange(ref _state, 1) != 0)
            return;

        try
        {
            Cancellation.Cancel();
            _onDisposed?.Invoke();
        }
        finally
        {
            try
            {
                enumerator.Dispose();
            }
            finally
            {
                Cancellation.Dispose();
            }
        }
    }
}
