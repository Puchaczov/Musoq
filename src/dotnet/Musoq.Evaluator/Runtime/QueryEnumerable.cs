using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Musoq.Evaluator.Runtime;

public sealed class QueryEnumerable<T> : IQueryRows<T>
{
    private readonly Func<CancellationToken, IEnumerable<T>> _rowsFactory;
    private readonly CancellationToken _token;
    private readonly Action? _onCompleted;
    private readonly Action<Exception>? _onException;
    private readonly Action? _onDisposed;
    private int _enumerated;

    public QueryEnumerable(
        Func<CancellationToken, IEnumerable<T>> rowsFactory,
        CancellationToken token,
        Action? onCompleted = null,
        Action<Exception>? onException = null,
        Action? onDisposed = null)
    {
        _rowsFactory = rowsFactory ?? throw new ArgumentNullException(nameof(rowsFactory));
        _token = token;
        _onCompleted = onCompleted;
        _onException = onException;
        _onDisposed = onDisposed;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var scope = CreateScope();
        try
        {
            return new QueryRowsEnumerator<T>(scope.Rows.GetEnumerator(), scope);
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private QueryRowsScope<T> CreateScope()
    {
        if (Interlocked.Exchange(ref _enumerated, 1) != 0)
            throw new InvalidOperationException("Query rows can be enumerated only once.");

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_token);
        try
        {
            var rows = _rowsFactory(cancellation.Token);
            return new QueryRowsScope<T>(rows, cancellation, _onCompleted, _onException, _onDisposed);
        }
        catch (Exception ex)
        {
            cancellation.Dispose();
            _onException?.Invoke(ex);
            throw;
        }
    }
}
