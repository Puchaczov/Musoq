using System;
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
        string? details)
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
    }

    /// <summary>Stable error code (e.g., MQ3022).</summary>
    public DiagnosticCode Code { get; }

    /// <summary>Stable code as display string (e.g., "MQ3022").</summary>
    public string CodeString => $"MQ{(int)Code}";

    /// <summary>Error or warning.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Compilation phase: parse, bind, runtime, datasource, feature-gate.</summary>
    public DiagnosticPhase Phase { get; }

    /// <summary>Human-readable summary of the problem.</summary>
    public string Message { get; }

    /// <summary>1-based line number, or null if unknown.</summary>
    public int? Line { get; }

    /// <summary>1-based column number, or null if unknown.</summary>
    public int? Column { get; }

    /// <summary>Length of the error span in characters, or null if unknown.</summary>
    public int? Length { get; }

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

    /// <summary>
    ///     Creates an envelope from a <see cref="Diagnostic" /> and optional source query text.
    /// </summary>
    public static MusoqErrorEnvelope FromDiagnostic(Diagnostic diagnostic, string? queryText = null)
    {
        var metadata = ErrorMetadataCatalog.Get(diagnostic.Code);

        var explanation = diagnostic.Explanation
                          ?? metadata?.Explanation;

        var docsRef = diagnostic.DocsReference
                      ?? metadata?.DocsReference;

        var fixes = BuildSuggestedFixes(diagnostic, metadata);

        string? snippet = diagnostic.ContextSnippet;
        if (snippet == null && queryText != null && diagnostic.Location.IsValid && !IsEmptySpanAtOrigin(diagnostic))
        {
            var sourceText = new SourceText(queryText);
            snippet = sourceText.GetContextSnippet(diagnostic.Span);
        }

        int? line = diagnostic.Location.IsValid && !IsEmptySpanAtOrigin(diagnostic) ? diagnostic.Location.Line : null;
        int? column = diagnostic.Location.IsValid && !IsEmptySpanAtOrigin(diagnostic) ? diagnostic.Location.Column : null;
        var spanLength = diagnostic.Span.Length;
        int? length = spanLength > 0 ? spanLength : null;

        return new MusoqErrorEnvelope(
            diagnostic.Code,
            diagnostic.Severity,
            diagnostic.Phase,
            diagnostic.Message,
            line,
            column,
            length,
            snippet,
            explanation,
            fixes,
            docsRef,
            details: null);
    }

    /// <summary>
    ///     Creates an envelope from an exception and optional source query text.
    /// </summary>
    public static MusoqErrorEnvelope FromException(Exception exception, string? queryText = null)
    {
        var sourceText = queryText != null ? new SourceText(queryText) : null;
        var diagnostic = exception.ToDiagnosticOrGeneric(sourceText);
        var envelope = FromDiagnostic(diagnostic, queryText);

        var details = exception.InnerException?.Message ?? exception.StackTrace;

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
            details);
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

    private static bool IsEmptySpanAtOrigin(Diagnostic diagnostic)
    {
        return diagnostic.Location.Offset == 0 && diagnostic.Span.Length == 0;
    }
}
