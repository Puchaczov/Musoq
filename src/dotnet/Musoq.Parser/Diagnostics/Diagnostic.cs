using System;
using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Represents a compiler diagnostic (error, warning, info, or hint).
/// </summary>
public sealed class Diagnostic
{
    private readonly List<string> _relatedInfo;
    private readonly List<DiagnosticAction> _suggestedFixes;

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
        IEnumerable<DiagnosticAction>? suggestedFixes = null)
    {
        Code = code;
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        EndLocation = endLocation ?? location;
        ContextSnippet = contextSnippet;
        _relatedInfo = relatedInfo != null ? new List<string>(relatedInfo) : new List<string>();
        _suggestedFixes = suggestedFixes != null
            ? new List<DiagnosticAction>(suggestedFixes)
            : new List<DiagnosticAction>();
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
    ///     Gets the text span from location information.
    /// </summary>
    public TextSpan Span => new(Location.Offset, EndLocation.Offset - Location.Offset);

    /// <summary>
    ///     Creates a copy of this diagnostic with additional related info.
    /// </summary>
    public Diagnostic WithRelatedInfo(string info)
    {
        var newRelatedInfo = new List<string>(_relatedInfo) { info };
        return new Diagnostic(Code, Severity, Message, Location, EndLocation, ContextSnippet,
            newRelatedInfo, _suggestedFixes);
    }

    /// <summary>
    ///     Creates a copy of this diagnostic with a suggested fix.
    /// </summary>
    public Diagnostic WithSuggestedFix(DiagnosticAction action)
    {
        var newFixes = new List<DiagnosticAction>(_suggestedFixes) { action };
        return new Diagnostic(Code, Severity, Message, Location, EndLocation, ContextSnippet,
            _relatedInfo, newFixes);
    }

    /// <summary>
    ///     Returns a formatted string representation.
    /// </summary>
    public override string ToString()
    {
        var severityStr = Severity.ToString().ToLowerInvariant();
        return $"{severityStr} {CodeString}: {Message} at {Location}";
    }

    /// <summary>
    ///     Returns a detailed formatted representation with context.
    /// </summary>
    public string ToDetailedString()
    {
        var lines = new List<string>
        {
            $"{Severity.ToString().ToLowerInvariant()} {CodeString}: {Message}",
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

    // Factory methods for common diagnostics

    /// <summary>
    ///     Creates an error diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Error(DiagnosticCode code, string message, TextSpan span)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(code, DiagnosticSeverity.Error, message, location, endLocation);
    }

    /// <summary>
    ///     Creates an error diagnostic.
    /// </summary>
    public static Diagnostic Error(DiagnosticCode code, string message, SourceLocation location,
        SourceLocation? endLocation = null)
    {
        return new Diagnostic(code, DiagnosticSeverity.Error, message, location, endLocation);
    }

    /// <summary>
    ///     Creates a warning diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Warning(DiagnosticCode code, string message, TextSpan span)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(code, DiagnosticSeverity.Warning, message, location, endLocation);
    }

    /// <summary>
    ///     Creates a warning diagnostic.
    /// </summary>
    public static Diagnostic Warning(DiagnosticCode code, string message, SourceLocation location,
        SourceLocation? endLocation = null)
    {
        return new Diagnostic(code, DiagnosticSeverity.Warning, message, location, endLocation);
    }

    /// <summary>
    ///     Creates an info diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Info(DiagnosticCode code, string message, TextSpan span)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(code, DiagnosticSeverity.Info, message, location, endLocation);
    }

    /// <summary>
    ///     Creates an info diagnostic.
    /// </summary>
    public static Diagnostic Info(DiagnosticCode code, string message, SourceLocation location,
        SourceLocation? endLocation = null)
    {
        return new Diagnostic(code, DiagnosticSeverity.Info, message, location, endLocation);
    }

    /// <summary>
    ///     Creates a hint diagnostic from a TextSpan.
    /// </summary>
    public static Diagnostic Hint(DiagnosticCode code, string message, TextSpan span)
    {
        var location = new SourceLocation(span.Start, 1, span.Start + 1);
        var endLocation = new SourceLocation(span.End, 1, span.End + 1);
        return new Diagnostic(code, DiagnosticSeverity.Hint, message, location, endLocation);
    }

    /// <summary>
    ///     Creates a hint diagnostic.
    /// </summary>
    public static Diagnostic Hint(DiagnosticCode code, string message, SourceLocation location,
        SourceLocation? endLocation = null)
    {
        return new Diagnostic(code, DiagnosticSeverity.Hint, message, location, endLocation);
    }
}
