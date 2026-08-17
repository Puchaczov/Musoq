using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Spec-compliant error envelope bundling all fields needed for
///     user-facing error display across CLI, server, and IDE contexts.
/// </summary>
public sealed class MusoqErrorEnvelope
{
    /// <summary>
    ///     Creates a new error envelope.
    /// </summary>
    public MusoqErrorEnvelope(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        DiagnosticPhase phase,
        string message,
        int? line,
        int? column,
        int? length,
        string? snippet,
        string? explanation,
        IReadOnlyList<string> suggestedFixes,
        string? docsReference,
        string? details,
        IReadOnlyList<DiagnosticAction>? actions = null,
        DiagnosticSourceKind sourceKind = DiagnosticSourceKind.Query,
        int? offset = null,
        int? endOffset = null,
        int? endLine = null,
        int? endColumn = null,
        IReadOnlyDictionary<string, string>? arguments = null,
        IReadOnlyList<DiagnosticRelatedLocation>? relatedLocations = null,
        string? correlationId = null)
    {
        Code = code;
        Severity = severity;
        Phase = phase;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Line = line;
        Column = column;
        Length = length;
        Snippet = snippet;
        Explanation = explanation;
        SuggestedFixes = suggestedFixes ?? Array.Empty<string>();
        DocsReference = docsReference;
        Details = details;
        Actions = actions ?? Array.Empty<DiagnosticAction>();
        SourceKind = sourceKind;
        Offset = offset;
        EndOffset = endOffset;
        EndLine = endLine;
        EndColumn = endColumn;
        Arguments = arguments ?? new Dictionary<string, string>(StringComparer.Ordinal);
        RelatedLocations = relatedLocations ?? Array.Empty<DiagnosticRelatedLocation>();
        CorrelationId = correlationId;
    }

    /// <summary>Stable error code (e.g., MQ3022).</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Stable code as display string (e.g., "MQ3022").</summary>
    public string CodeString => $"MQ{(int)Code}";

    /// <summary>Error or warning.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Compilation phase where the diagnostic originated.</summary>
    public DiagnosticPhase Phase { get; }

    /// <summary>Source domain containing the primary diagnostic location.</summary>
    public DiagnosticSourceKind SourceKind { get; }

    /// <summary>Human-readable summary of the problem.</summary>
    public string Message { get; }

    /// <summary>1-based line number, or null if unknown.</summary>
    public int? Line { get; }

    /// <summary>1-based column number, or null if unknown.</summary>
    public int? Column { get; }

    /// <summary>Length of the error span in characters; zero is a known insertion span.</summary>
    public int? Length { get; }

    /// <summary>Absolute start offset, or null when the location is unknown.</summary>
    public int? Offset { get; }

    /// <summary>Absolute exclusive end offset, or null when the location is unknown.</summary>
    public int? EndOffset { get; }

    /// <summary>1-based end line, or null when the end location is unknown.</summary>
    public int? EndLine { get; }

    /// <summary>1-based end column, or null when the end location is unknown.</summary>
    public int? EndColumn { get; }

    /// <summary>Source snippet with pointer (if available).</summary>
    public string? Snippet { get; }

    /// <summary>Plain-language explanation of why this error occurred.</summary>
    public string? Explanation { get; }

    /// <summary>Concrete fix suggestions (max 2-3).</summary>
    public IReadOnlyList<string> SuggestedFixes { get; }

    /// <summary>Documentation section or page reference.</summary>
    public string? DocsReference { get; }

    /// <summary>Internal diagnostic detail (shown with --verbose or in Details: section).</summary>
    public string? Details { get; }

    /// <summary>Stable machine-readable string facts associated with the diagnostic.</summary>
    public IReadOnlyDictionary<string, string> Arguments { get; }

    /// <summary>Typed secondary locations associated with the diagnostic.</summary>
    public IReadOnlyList<DiagnosticRelatedLocation> RelatedLocations { get; }

    /// <summary>Structured actions, including optional text edits.</summary>
    public IReadOnlyList<DiagnosticAction> Actions { get; }

    /// <summary>Optional correlation identifier for internal failures.</summary>
    public string? CorrelationId { get; }

    /// <summary>
    ///     Creates an envelope from a <see cref="Diagnostic" /> and optional source query text.
    /// </summary>
    public static MusoqErrorEnvelope FromDiagnostic(Diagnostic diagnostic, string? queryText = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var metadata = ErrorMetadataCatalog.Get(diagnostic.Code);

        var explanation = diagnostic.Explanation
                          ?? metadata?.Explanation;

        var docsRef = diagnostic.DocsReference
                      ?? metadata?.DocsReference;

        var fixes = BuildSuggestedFixes(diagnostic, metadata);
        var actions = diagnostic.SuggestedFixes.Count > 0
            ? diagnostic.SuggestedFixes
            : DiagnosticDescriptorRegistry.Get(diagnostic.Code)?.DefaultActions ?? [];

        string? snippet = diagnostic.ContextSnippet;
        var hasLocation = diagnostic.Location.IsValid;
        var hasEndLocation = diagnostic.EndLocation.IsValid;
        var spanLength = hasLocation && hasEndLocation && diagnostic.EndLocation.Offset >= diagnostic.Location.Offset
            ? diagnostic.Span.Length
            : (int?)null;

        if (snippet == null && queryText != null && hasLocation)
        {
            var sourceText = new SourceText(queryText);
            snippet = sourceText.GetContextSnippet(diagnostic.Span);
        }

        int? line = hasLocation ? diagnostic.Location.Line : null;
        int? column = hasLocation ? diagnostic.Location.Column : null;
        int? offset = hasLocation ? diagnostic.Location.Offset : null;
        int? endOffset = hasEndLocation ? diagnostic.EndLocation.Offset : null;
        int? endLine = hasEndLocation ? diagnostic.EndLocation.Line : null;
        int? endColumn = hasEndLocation ? diagnostic.EndLocation.Column : null;

        return new MusoqErrorEnvelope(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Phase,
            diagnostic.Message,
            line,
            column,
            spanLength,
            snippet,
            explanation,
            fixes,
            docsRef,
            details: null,
            actions: actions,
            sourceKind: diagnostic.SourceKind,
            offset: offset,
            endOffset: endOffset,
            endLine: endLine,
            endColumn: endColumn,
            arguments: diagnostic.Arguments,
            relatedLocations: diagnostic.RelatedLocations,
            correlationId: diagnostic.CorrelationId);
    }

    /// <summary>
    ///     Creates an envelope from an exception and optional source query text.
    /// </summary>
    public static MusoqErrorEnvelope FromException(
        Exception exception,
        string? queryText = null,
        bool includeSensitiveDetails = false)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var sourceText = queryText != null ? new SourceText(queryText) : null;
        var diagnostic = exception.ToDiagnosticOrGeneric(sourceText);
        var envelope = FromDiagnostic(diagnostic, queryText);

        var details = includeSensitiveDetails ? GetExceptionDetails(exception) : null;

        return new MusoqErrorEnvelope(
            envelope.Code,
            envelope.Severity,
            envelope.Phase,
            envelope.Message,
            envelope.Line,
            envelope.Column,
            envelope.Length,
            envelope.Snippet,
            envelope.Explanation,
            envelope.SuggestedFixes,
            envelope.DocsReference,
            details,
            envelope.Actions,
            envelope.SourceKind,
            envelope.Offset,
            envelope.EndOffset,
            envelope.EndLine,
            envelope.EndColumn,
            envelope.Arguments,
            envelope.RelatedLocations,
            envelope.CorrelationId);
    }

    /// <summary>
    ///     Creates an envelope with raw exception details for explicit debugging
    ///     or trusted local tooling.
    /// </summary>
    public static MusoqErrorEnvelope FromExceptionVerbose(Exception exception, string? queryText = null)
    {
        return FromException(exception, queryText, includeSensitiveDetails: true);
    }

    private static string[] BuildSuggestedFixes(Diagnostic diagnostic, ErrorMetadata? metadata)
    {
        var fixes = new List<string>();

        foreach (var fix in diagnostic.SuggestedFixes)
            fixes.Add(fix.Title);

        if (fixes.Count == 0 && metadata?.SuggestedFixes != null)
            fixes.AddRange(metadata.SuggestedFixes);

        return fixes.ToArray();
    }

    private static string? GetExceptionDetails(Exception exception)
    {
        return exception.InnerException?.Message ?? exception.StackTrace;
    }
}
