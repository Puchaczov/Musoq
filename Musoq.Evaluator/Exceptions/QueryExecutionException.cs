using System;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when query execution fails with detailed error information.
///     Provides context about what went wrong during query execution.
/// </summary>
public class QueryExecutionException : InvalidOperationException
{
    public QueryExecutionException(string queryContext, string executionPhase, string message)
        : base(message)
    {
        QueryContext = queryContext;
        ExecutionPhase = executionPhase;
    }

    public QueryExecutionException(string queryContext, string executionPhase, string message, Exception innerException)
        : base(message, innerException)
    {
        QueryContext = queryContext;
        ExecutionPhase = executionPhase;
    }

    public string QueryContext { get; }
    public string ExecutionPhase { get; }

    public static QueryExecutionException ForNullRunnable()
    {
        return new QueryExecutionException(
            "CompiledQuery",
            "Initialization",
            "The query execution object (IRunnable) cannot be null. This indicates a problem with query compilation. Please check your SQL query syntax and try again."
        );
    }

    public static QueryExecutionException ForExecutionFailure(string phase, Exception innerException)
    {
        return new QueryExecutionException(
            "CompiledQuery",
            phase,
            $"Query execution failed during {phase}: {innerException.Message}. Please check your query and data sources for issues.",
            innerException
        );
    }

    public static QueryExecutionException ForCancellationFailure(string phase, Exception innerException)
    {
        return new QueryExecutionException(
            "CompiledQuery",
            phase,
            $"Query execution was cancelled during {phase}, but cleanup failed: {innerException.Message}",
            innerException
        );
    }
}