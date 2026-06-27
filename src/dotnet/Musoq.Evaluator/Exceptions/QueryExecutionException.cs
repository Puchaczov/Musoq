using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when query execution fails with detailed error information.
///     Provides context about what went wrong during query execution.
/// </summary>
public class QueryExecutionException : InvalidOperationException
{
    public QueryExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public QueryExecutionException(string message)
        : base(message)
    {
    }

    public QueryExecutionException()
    {
    }
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

    private QueryExecutionException(
        string queryContext,
        string executionPhase,
        MusoqErrorEnvelope envelope,
        Exception innerException)
        : base(MusoqErrorEnvelopeFormatter.FormatText(envelope), innerException)
    {
        QueryContext = queryContext;
        ExecutionPhase = executionPhase;
        Envelope = envelope;
    }

    public string QueryContext { get; } = string.Empty;
    public string ExecutionPhase { get; } = string.Empty;
    public MusoqErrorEnvelope? Envelope { get; }

    public string FormatText()
    {
        return Envelope != null
            ? MusoqErrorEnvelopeFormatter.FormatText(Envelope)
            : Message;
    }

    public string FormatJson()
    {
        return Envelope != null
            ? MusoqErrorEnvelopeFormatter.FormatJson(Envelope)
            : MusoqErrorEnvelopeFormatter.FormatJson(MusoqErrorEnvelope.FromException(this));
    }

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
        ArgumentNullException.ThrowIfNull(innerException);
        return new QueryExecutionException(
            "CompiledQuery",
            phase,
            $"Query execution failed during {phase}: {innerException.Message}. Please check your query and data sources for issues.",
            innerException
        );
    }

    public static QueryExecutionException ForScriptParameterBinding(ScriptParameterBindingException innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(innerException.ToDiagnostic());
        return new QueryExecutionException(
            "CompiledQuery",
            "ScriptParameterBinding",
            envelope,
            innerException);
    }

    public static QueryExecutionException ForCancellationFailure(string phase, Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        return new QueryExecutionException(
            "CompiledQuery",
            phase,
            $"Query execution was cancelled during {phase}, but cleanup failed: {innerException.Message}",
            innerException
        );
    }
}
