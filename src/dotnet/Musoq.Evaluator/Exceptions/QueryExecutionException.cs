using Musoq.Parser.Diagnostics;
using Musoq.Schema.Exceptions;

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

        if (innerException is QueryExecutionException existing)
            return existing;

        var diagnostic = InternalDiagnosticException.ForExecution(innerException);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic.ToDiagnostic());
        return new QueryExecutionException(
            "CompiledQuery",
            phase,
            envelope,
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

    public static QueryExecutionException ForDataSourceFailure(DataSourceLifecycleException innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(innerException.ToDiagnostic());
        return new QueryExecutionException(
            "CompiledQuery",
            innerException.Operation,
            envelope,
            innerException);
    }

    public string FormatVerboseText()
    {
        return FormatText() + Environment.NewLine + InnerException;
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
