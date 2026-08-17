using System.Collections;
using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Runtime;

internal sealed class QueryRowsEnumerator<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    private readonly QueryRowsScope<T> _scope;
    private bool _finished;

    public QueryRowsEnumerator(IEnumerator<T> inner, QueryRowsScope<T> scope)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public T Current => _inner.Current;

    object? IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_finished)
            return false;

        try
        {
            if (_inner.MoveNext())
                return true;

            _finished = true;
            _inner.Dispose();
            _scope.Complete();
            return false;
        }
        catch (Exception ex)
        {
            _finished = true;
            _inner.Dispose();
            _scope.Fail(ex);
            throw ExecutionFailureConverter.Convert("RowEnumeration", ex);
        }
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        if (_finished)
            return;

        _finished = true;
        try
        {
            _scope.DisposeEnumerator(_inner);
        }
        catch (Exception exception)
        {
            throw ExecutionFailureConverter.Convert("RowEnumeration", exception);
        }
    }
}
