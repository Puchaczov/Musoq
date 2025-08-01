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
        
        try
        {
            var table = Run(exitSourcesLoaderTokenSource.Token);
            
            try
            {
                exitSourcesLoaderTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                // Log cancellation failure but don't fail the query since we got results
                throw QueryExecutionException.ForCancellationFailure("cleanup", ex);
            }
            
            return table;
        }
        catch (Exception ex) when (!(ex is QueryExecutionException))
        {
            try
            {
                exitSourcesLoaderTokenSource.Cancel();
            }
            catch
            {
                // Ignore cleanup failures when main execution already failed
            }
            
            throw QueryExecutionException.ForExecutionFailure("synchronous execution", ex);
        }
    }

    public Table Run(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            throw new OperationCanceledException("Query execution was cancelled before it started.", token);

        try
        {
            var result = _runnable.Run(token);
            
            if (result == null)
                throw QueryExecutionException.ForExecutionFailure("result generation", 
                    new InvalidOperationException("Query execution returned null result"));
            
            return result;
        }
        catch (OperationCanceledException)
        {
            throw; // Re-throw cancellation exceptions as-is
        }
        catch (Exception ex) when (!(ex is QueryExecutionException))
        {
            throw QueryExecutionException.ForExecutionFailure("query execution with cancellation token", ex);
        }
    }
}