using System;
using System.Diagnostics;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator;

[DebuggerStepThrough]
public class CompiledQuery
{
    private readonly IRunnable _runnable;

    public CompiledQuery(IRunnable runnable)
    {
        _runnable = runnable ?? throw QueryExecutionException.ForNullRunnable();
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