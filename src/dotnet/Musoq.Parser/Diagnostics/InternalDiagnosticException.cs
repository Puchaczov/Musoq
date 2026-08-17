using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     A sanitized, typed wrapper for an engine invariant failure.
///     The original exception remains available as <see cref="Exception.InnerException"/>
///     to trusted diagnostics code, but is not copied into the default user payload.
/// </summary>
public sealed class InternalDiagnosticException : Exception, IDiagnosticException
{
    private InternalDiagnosticException(
        DiagnosticCode code,
        DiagnosticPhase phase,
        string message,
        DiagnosticSourceKind sourceKind,
        string correlationId,
        TextSpan? span,
        Exception innerException)
        : base(message, innerException)
    {
        Code = code;
        Phase = phase;
        SourceKind = sourceKind;
        CorrelationId = correlationId;
        Span = span;
    }

    /// <summary>Gets the stable internal diagnostic code.</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Gets the explicit phase of the internal failure.</summary>
    public DiagnosticPhase Phase { get; }

    /// <summary>Gets the source domain of the internal failure.</summary>
    public DiagnosticSourceKind SourceKind { get; }

    /// <summary>Gets the correlation identifier safe to share with support tooling.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the optional originating query span.</summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Wraps an unexpected compiler-side exception with a sanitized message.
    /// </summary>
    public static InternalDiagnosticException ForCompiler(Exception innerException, TextSpan? span = null)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        var correlationId = CreateCorrelationId();
        return new InternalDiagnosticException(
            DiagnosticCode.MQ9001_InternalCompilerError,
            DiagnosticPhase.Internal,
            CreateMessage(DiagnosticCode.MQ9001_InternalCompilerError, correlationId),
            DiagnosticSourceKind.Internal,
            correlationId,
            span,
            innerException);
    }

    /// <summary>
    ///     Wraps an unexpected execution-side exception with a sanitized message.
    /// </summary>
    public static InternalDiagnosticException ForExecution(Exception innerException, TextSpan? span = null)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        var correlationId = CreateCorrelationId();
        return new InternalDiagnosticException(
            DiagnosticCode.MQ9002_InternalExecutionError,
            DiagnosticPhase.Internal,
            CreateMessage(DiagnosticCode.MQ9002_InternalExecutionError, correlationId),
            DiagnosticSourceKind.Internal,
            correlationId,
            span,
            innerException);
    }

    /// <inheritdoc />
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var (location, endLocation) = ResolveLocations(sourceText, Span);
        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            location,
            endLocation,
            phase: Phase,
            sourceKind: SourceKind,
            arguments: new[]
            {
                new KeyValuePair<string, string>("correlationId", CorrelationId),
                new KeyValuePair<string, string>(
                    "exceptionType",
                    InnerException?.GetType().FullName ?? GetType().FullName ?? "unknown")
            },
            correlationId: CorrelationId);
    }

    private static (SourceLocation Start, SourceLocation End) ResolveLocations(SourceText? sourceText, TextSpan? span)
    {
        if (span is { } knownSpan && sourceText != null)
            return sourceText.GetLocations(knownSpan);

        if (span is { } knownOffset)
        {
            var start = new SourceLocation(knownOffset.Start, 1, knownOffset.Start + 1);
            var end = new SourceLocation(knownOffset.End, 1, knownOffset.End + 1);
            return (start, end);
        }

        return (SourceLocation.None, SourceLocation.None);
    }

    private static string CreateCorrelationId()
    {
        return $"internal-{Guid.NewGuid():N}";
    }

    private static string CreateMessage(DiagnosticCode code, string correlationId)
    {
        return ErrorCatalog.GetMessage(code, correlationId);
    }
}
