using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public sealed class QueryTableEnumerable<TRow> : ITableRowBatchSource<TRow>
    where TRow : Row
{
    private readonly Func<CancellationToken, IEnumerable<TRow>> _rowsFactory;
    private readonly CancellationToken _token;
    private readonly Action? _onCompleted;
    private readonly Action<Exception>? _onException;
    private readonly Action? _onDisposed;
    private int _enumerated;

    public QueryTableEnumerable(
        Func<CancellationToken, IEnumerable<TRow>> rowsFactory,
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

    public void AddTo(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        using var scope = CreateScope();
        try
        {
            if (scope.Rows is ITableRowBatchSource<TRow> batchSource)
            {
                batchSource.AddTo(table);
                scope.Complete();
                return;
            }

            if (scope.Rows.TryGetNonEnumeratedCount(out var count))
                table.EnsureCapacity(table.Count + count);

            foreach (var row in scope.Rows)
                table.AddDirect(row);

            scope.Complete();
        }
        catch (Exception ex)
        {
            scope.Fail(ex);
            throw;
        }
    }

    public IEnumerator<TRow> GetEnumerator()
    {
        var scope = CreateScope();
        try
        {
            return new QueryRowsEnumerator<TRow>(scope.Rows.GetEnumerator(), scope);
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

    private QueryRowsScope<TRow> CreateScope()
    {
        if (Interlocked.Exchange(ref _enumerated, 1) != 0)
            throw new InvalidOperationException("Query rows can be enumerated only once.");

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_token);
        try
        {
            var rows = _rowsFactory(cancellation.Token);
            return new QueryRowsScope<TRow>(rows, cancellation, _onCompleted, _onException, _onDisposed);
        }
        catch (Exception ex)
        {
            cancellation.Dispose();
            _onException?.Invoke(ex);
            throw;
        }
    }
}
