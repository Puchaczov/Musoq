using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents a compiler diagnostic (error, warning, info, or hint).
/// </summary>
public sealed class Diagnostic
{
    private readonly List<string> _relatedInfo;
    private readonly List<DiagnosticAction> _suggestedFixes;
    private readonly Dictionary<string, string> _arguments;
    private readonly List<DiagnosticRelatedLocation> _relatedLocations;

    /// <summary>
    ///     Creates a new diagnostic.
    /// </summary>
    public Diagnostic(
        DiagnosticCode code,
        DiagnosticSeverity severity,
        string message,
        SourceLocation location,
        SourceLocation? endLocation = null,
        string? contextSnippet = null,
        IEnumerable<string>? relatedInfo = null,
        IEnumerable<DiagnosticAction>? suggestedFixes = null,
        string? explanation = null,
        string? docsReference = null,
        DiagnosticPhase? phase = null,
        DiagnosticSourceKind? sourceKind = null,
        IEnumerable<KeyValuePair<string, string>>? arguments = null,
        IEnumerable<DiagnosticRelatedLocation>? relatedLocations = null,
        string? correlationId = null)
    {
        Code = code;
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        EndLocation = endLocation ?? location;
        ContextSnippet = contextSnippet;
        _relatedInfo = relatedInfo != null ? [..relatedInfo] : [];
        _suggestedFixes = suggestedFixes != null
            ? [..suggestedFixes]
            : [];
        _arguments = arguments != null
            ? new Dictionary<string, string>(arguments, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        _relatedLocations = relatedLocations != null
            ? [..relatedLocations]
            : [];
        Explanation = explanation;
        DocsReference = docsReference;
        _phase = phase ?? DiagnosticPhaseMapping.FromCode(code);
        SourceKind = sourceKind ?? DiagnosticSourceKindMapping.FromCode(code);
        CorrelationId = correlationId;
    }

    /// <summary>
    ///     Gets the diagnostic code.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the diagnostic code as a string (e.g., "MQ2001").
    /// </summary>
    public string CodeString => $"MQ{(int)Code}";

    /// <summary>
    ///     Gets the severity level.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    ///     Gets the diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the start location in source.
    /// </summary>
    public SourceLocation Location { get; }

    /// <summary>
    ///     Gets the end location in source.
    /// </summary>
    public SourceLocation EndLocation { get; }

    /// <summary>
    ///     Gets the optional context snippet showing the error in source.
    /// </summary>
    public string? ContextSnippet { get; }

    /// <summary>
    ///     Gets related information messages.
    /// </summary>
    public IReadOnlyList<string> RelatedInfo => _relatedInfo;

    /// <summary>
    ///     Gets suggested fixes or actions.
    /// </summary>
    public IReadOnlyList<DiagnosticAction> SuggestedFixes => _suggestedFixes;

    /// <summary>
    ///     Returns true if this is an error.
    /// </summary>
    public bool IsError => Severity == DiagnosticSeverity.Error;

    /// <summary>
    ///     Returns true if this is a warning.
    /// </summary>
    public bool IsWarning => Severity == DiagnosticSeverity.Warning;

    /// <summary>
    ///     Gets a plain-language explanation of why this error occurred.
    /// </summary>
    public string? Explanation { get; }

    /// <summary>
    ///     Gets a documentation reference for this diagnostic (e.g., spec section or doc page id).
    /// </summary>
    public string? DocsReference { get; }

    /// <summary>
    ///     Gets the explicit phase where the diagnostic originated.
    /// </summary>
    private readonly DiagnosticPhase _phase;

    public DiagnosticPhase Phase => _phase;

    /// <summary>
    ///     Gets the source domain containing the primary location.
    /// </summary>
    public DiagnosticSourceKind SourceKind { get; }

    /// <summary>
    ///     Gets stable string-valued facts associated with the diagnostic.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments => _arguments;

    /// <summary>
    ///     Gets typed secondary locations associated with the diagnostic.
    /// </summary>
    public IReadOnlyList<DiagnosticRelatedLocation> RelatedLocations => _relatedLocations;

    /// <summary>
    ///     Gets an optional identifier used to correlate internal failures.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    ///     Gets the text span from location information.
    /// </summary>
    public TextSpan Span => new(Location.Offset, EndLocation.Offset - Location.Offset);

    /// <summary>
    ///     Creates a copy of this diagnostic with additional related info.
    /// </summary>
    public Diagnostic WithRelatedInfo(string info)
    {
        var newRelatedInfo = new List<string>(_relatedInfo) { info };
        return Copy(relatedInfo: newRelatedInfo);
    }

    /// <summary>
    ///     Creates a copy of this diagnostic with a suggested fix.
    /// </summary>
    public Diagnostic WithSuggestedFix(DiagnosticAction action)
    {
        var newFixes = new List<DiagnosticAction>(_suggestedFixes) { action };
        return Copy(suggestedFixes: newFixes);
    }

    /// <summary>
    ///     Creates a copy of this diagnostic with an explanation.
    /// </summary>
    public Diagnostic WithExplanation(string explanation)
    {
        return Copy(explanation: explanation);
    }

    /// <summary>
    ///     Creates a copy of this diagnostic with a documentation reference.
    /// </summary>
    public Diagnostic WithDocsReference(string docsReference)
    {
        return Copy(docsReference: docsReference);
    }

    /// <summary>
    ///     Creates a copy with a complete source location while retaining all
    ///     structured diagnostic payload.
    /// </summary>
    public Diagnostic WithLocations(SourceLocation location, SourceLocation endLocation)
    {
        return Copy(location: location, endLocation: endLocation);
    }

    /// <summary>
    ///     Creates a copy whose locations and context snippet are resolved from
    ///     one source span. A zero-length span is preserved as a known
    ///     insertion point; callers must use <c>null</c> when the location is
    ///     genuinely unknown.
    /// </summary>
    public Diagnostic WithSourceContext(SourceText? sourceText, TextSpan span)
    {
        var locations = sourceText != null
            ? sourceText.GetLocations(span)
            : (Start: new SourceLocation(span.Start, 1, span.Start + 1),
                End: new SourceLocation(span.End, 1, span.End + 1));
        var contextSnippet = sourceText != null && locations.Start.IsValid
            ? sourceText.GetContextSnippet(span)
            : null;

        return Copy(
            location: locations.Start,
            endLocation: locations.End,
            contextSnippet: contextSnippet);
    }

    /// <summary>
    ///     Creates a copy with an additional structured argument.
    /// </summary>
    public Diagnostic WithArgument(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        var arguments = new Dictionary<string, string>(_arguments, StringComparer.Ordinal)
        {
            [name] = value
        };
        return Copy(arguments: arguments);
    }

    /// <summary>
    ///     Creates a copy with an additional typed related location.
    /// </summary>
    public Diagnostic WithRelatedLocation(DiagnosticRelatedLocation relatedLocation)
    {
        ArgumentNullException.ThrowIfNull(relatedLocation);
        var locations = new List<DiagnosticRelatedLocation>(_relatedLocations) { relatedLocation };
        return Copy(relatedLocations: locations);
    }

    /// <summary>
    ///     Returns a formatted string representation.
    /// </summary>
    public override string ToString()
    {
        var severityStr = FormatSeverity(Severity);
        return $"{severityStr} {CodeString}: {Message} at {Location}";
    }

    /// <summary>
    ///     Returns a detailed formatted representation with context.
    /// </summary>
    public string ToDetailedString()
    {
        var lines = new List<string>
        {
            $"{FormatSeverity(Severity)} {CodeString}: {Message}",
            $"  --> {Location}"
        };

        if (!string.IsNullOrEmpty(ContextSnippet))
        {
            lines.Add("   |");
            foreach (var line in ContextSnippet.Split('\n')) lines.Add(line.TrimEnd('\r'));
        }

        foreach (var info in _relatedInfo) lines.Add($"  = note: {info}");

        foreach (var fix in _suggestedFixes) lines.Add($"  = help: {fix.Title}");

        return string.Join(Environment.NewLine, lines);
    }

    private Diagnostic Copy(
        SourceLocation? location = null,
        SourceLocation? endLocation = null,
        string? contextSnippet = null,
        IEnumerable<string>? relatedInfo = null,
        IEnumerable<DiagnosticAction>? suggestedFixes = null,
        string? explanation = null,
        string? docsReference = null,
        IEnumerable<KeyValuePair<string, string>>? arguments = null,
        IEnumerable<DiagnosticRelatedLocation>? relatedLocations = null)
    {
        return new Diagnostic(
            Code,
            Severity,
            Message,
            location ?? Location,
            endLocation ?? EndLocation,
            contextSnippet ?? ContextSnippet,
            relatedInfo ?? _relatedInfo,
            suggestedFixes ?? _suggestedFixes,
            explanation ?? Explanation,
            docsReference ?? DocsReference,
            _phase,
            SourceKind,
            arguments ?? _arguments,
            relatedLocations ?? _relatedLocations,
            CorrelationId);
    }

    private static string FormatSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => severity.ToString()
        };
    }

    // Factory methods for common diagnostics

    /// <summary>
    ///     Creates an error diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Error(
        DiagnosticCode code,
        string message,
        TextSpan span,
        DiagnosticSourceKind? sourceKind = null)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates an error diagnostic.
    /// </summary>
    public static Diagnostic Error(
        DiagnosticCode code,
        string message,
        SourceLocation location,
        SourceLocation? endLocation = null,
        DiagnosticSourceKind? sourceKind = null)
    {
        return new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates an error diagnostic whose source location is genuinely
    ///     unknown. This is distinct from a known zero-length insertion span.
    /// </summary>
    public static Diagnostic ErrorUnknownLocation(
        DiagnosticCode code,
        string message,
        DiagnosticSourceKind? sourceKind = null)
    {
        return new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            SourceLocation.None,
            SourceLocation.None,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates a warning diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Warning(
        DiagnosticCode code,
        string message,
        TextSpan span,
        DiagnosticSourceKind? sourceKind = null)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(
            code,
            DiagnosticSeverity.Warning,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates a warning diagnostic.
    /// </summary>
    public static Diagnostic Warning(
        DiagnosticCode code,
        string message,
        SourceLocation location,
        SourceLocation? endLocation = null,
        DiagnosticSourceKind? sourceKind = null)
    {
        return new Diagnostic(
            code,
            DiagnosticSeverity.Warning,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates an info diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Info(
        DiagnosticCode code,
        string message,
        TextSpan span,
        DiagnosticSourceKind? sourceKind = null)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(
            code,
            DiagnosticSeverity.Info,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates an info diagnostic.
    /// </summary>
    public static Diagnostic Info(
        DiagnosticCode code,
        string message,
        SourceLocation location,
        SourceLocation? endLocation = null,
        DiagnosticSourceKind? sourceKind = null)
    {
        return new Diagnostic(
            code,
            DiagnosticSeverity.Info,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates a hint diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Hint(
        DiagnosticCode code,
        string message,
        TextSpan span,
        DiagnosticSourceKind? sourceKind = null)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(
            code,
            DiagnosticSeverity.Hint,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }

    /// <summary>
    ///     Creates a hint diagnostic.
    /// </summary>
    public static Diagnostic Hint(
        DiagnosticCode code,
        string message,
        SourceLocation location,
        SourceLocation? endLocation = null,
        DiagnosticSourceKind? sourceKind = null)
    {
        return new Diagnostic(
            code,
            DiagnosticSeverity.Hint,
            message,
            location,
            endLocation,
            sourceKind: sourceKind);
    }
}
