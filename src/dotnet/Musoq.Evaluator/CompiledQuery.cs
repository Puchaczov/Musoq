using System;
using System.Diagnostics;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledQuery(IRunnable runnable)
{
    private readonly IRunnable _runnable = runnable ?? throw QueryExecutionException.ForNullRunnable();

    public event QueryPhaseEventHandler PhaseChanged
    {
        add => _runnable.PhaseChanged += value;
        remove => _runnable.PhaseChanged -= value;
    }

    public event DataSourceEventHandler DataSourceProgress
    {
        add => _runnable.DataSourceProgress += value;
        remove => _runnable.DataSourceProgress -= value;
    }

    public Table Run()
    {
        using var exitSourcesLoaderTokenSource = new CancellationTokenSource();

        var table = Run(exitSourcesLoaderTokenSource.Token);
        exitSourcesLoaderTokenSource.Cancel();

        return table;
    }

    public Table Run(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        var result = _runnable.Run(token);

        if (result == null)
            throw new InvalidOperationException("Query execution returned null result.");

        return result;
    }
}
