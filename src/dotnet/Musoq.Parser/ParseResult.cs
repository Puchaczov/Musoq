#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Parser;

/// <summary>
///     Represents the result of parsing a query, including the AST and any diagnostics.
///     This is the primary API for consuming parser output in an LSP-friendly way.
/// </summary>
public sealed class ParseResult
{
    private readonly List<Diagnostic> _diagnostics;

    /// <summary>
    ///     Creates a new parse result with a successful AST.
    /// </summary>
    public ParseResult(RootNode root, SourceText sourceText)
        : this(root, sourceText, Array.Empty<Diagnostic>())
    {
    }

    /// <summary>
    ///     Creates a new parse result with an AST and diagnostics.
    /// </summary>
    public ParseResult(RootNode? root, SourceText sourceText, IEnumerable<Diagnostic> diagnostics)
    {
        Root = root;
        SourceText = sourceText;
        _diagnostics = diagnostics.ToList();
    }

    /// <summary>
    ///     Gets the root node of the AST, or null if parsing completely failed.
    /// </summary>
    public RootNode? Root { get; }

    /// <summary>
    ///     Gets the source text that was parsed.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    ///     Gets all diagnostics (errors, warnings, etc.) from parsing.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    /// <summary>
    ///     Gets only error-level diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets only warning-level diagnostics.
    /// </summary>
    public IEnumerable<Diagnostic> Warnings => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Gets whether parsing was successful (no errors).
    /// </summary>
    public bool Success => Root != null && !HasErrors;

    /// <summary>
    ///     Gets whether there are any errors.
    /// </summary>
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets whether there are any warnings.
    /// </summary>
    public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Gets whether the AST is available (may be partial if there were errors).
    /// </summary>
    public bool HasAst => Root != null;

    /// <summary>
    ///     Gets the error count.
    /// </summary>
    public int ErrorCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets the warning count.
    /// </summary>
    public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    ///     Creates a failed parse result with only diagnostics.
    /// </summary>
    public static ParseResult Failed(SourceText sourceText, IEnumerable<Diagnostic> diagnostics)
    {
        return new ParseResult(null, sourceText, diagnostics);
    }

    /// <summary>
    ///     Gets diagnostics sorted by location.
    /// </summary>
    public IEnumerable<Diagnostic> GetSortedDiagnostics()
    {
        return _diagnostics.OrderBy(d => d.Location.Offset);
    }

    /// <summary>
    ///     Gets diagnostics at or near a specific position.
    /// </summary>
    /// <param name="offset">The character offset in the source.</param>
    /// <param name="tolerance">How far from the position to look.</param>
    public IEnumerable<Diagnostic> GetDiagnosticsAt(int offset, int tolerance = 0)
    {
        return _diagnostics.Where(d =>
            offset >= d.Location.Offset - tolerance &&
            offset <= d.EndLocation.Offset + tolerance);
    }

    /// <summary>
    ///     Gets diagnostics on a specific line (1-based).
    /// </summary>
    public IEnumerable<Diagnostic> GetDiagnosticsOnLine(int line)
    {
        return _diagnostics.Where(d => d.Location.Line == line);
    }

    /// <summary>
    ///     Formats all diagnostics for display.
    /// </summary>
    public string FormatDiagnostics()
    {
        if (_diagnostics.Count == 0)
            return "No diagnostics.";

        var formatter = new DiagnosticFormatter();
        return string.Join(Environment.NewLine, _diagnostics.Select(d => formatter.Format(d)));
    }

    /// <summary>
    ///     Throws if there are any errors.
    /// </summary>
    public void ThrowIfErrors()
    {
        if (HasErrors) throw new ParseException(FormatDiagnostics(), _diagnostics);
    }
}

/// <summary>
///     Exception thrown when parsing fails and ThrowIfErrors is called.
/// </summary>
public sealed class ParseException : Exception
{
    /// <summary>
    ///     Creates a new parse exception.
    /// </summary>
    public ParseException(string message, IEnumerable<Diagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics.ToList();
    }

    /// <summary>
    ///     Gets the diagnostics that caused the exception.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
