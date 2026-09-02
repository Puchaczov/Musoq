using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a column or alias cannot be resolved.
/// </summary>
public class UnknownColumnOrAliasException : Exception, IDiagnosticException
{

    public UnknownColumnOrAliasException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnknownColumnOrAliasException()
    {
    }
    /// <summary>
    ///     Initializes a new instance of UnknownColumnOrAliasException.
    /// </summary>
    public UnknownColumnOrAliasException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3001_UnknownColumn;
    }

    /// <summary>
    ///     Initializes a new instance of UnknownColumnOrAliasException with diagnostic information.
    /// </summary>
    public UnknownColumnOrAliasException(string columnName, string context, TextSpan span)
        : base($"Unknown column or alias '{columnName}'{(string.IsNullOrEmpty(context) ? "" : $" {context}")}.")
    {
        ColumnName = columnName;
        Code = DiagnosticCode.MQ3001_UnknownColumn;
        Span = span;
    }

    /// <summary>
    ///     Initializes an unknown column error with bounded candidate facts.
    /// </summary>
    public UnknownColumnOrAliasException(
        string columnName,
        string context,
        TextSpan span,
        IEnumerable<string> candidates)
        : this(columnName, context, span)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        AvailableColumns = NormalizeCandidates(candidates);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(columnName, AvailableColumns);
        Suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
    }

    /// <summary>
    ///     Gets the name of the unknown column or alias.
    /// </summary>
    public string? ColumnName { get; }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Gets the candidate columns captured while binding the reference.
    /// </summary>
    public IReadOnlyList<string> AvailableColumns { get; } = [];

    /// <summary>
    ///     Gets the unique close spelling candidate, when one exists.
    /// </summary>
    public string? Suggestion { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(ColumnName))
            arguments["column"] = ColumnName;
        if (AvailableColumns.Count > 0)
            arguments["availableColumns"] = string.Join(", ", AvailableColumns);
        if (!string.IsNullOrWhiteSpace(Suggestion))
            arguments["suggestion"] = Suggestion;

        var replacementSpan = GetIdentifierSpan(sourceText, span, ColumnName);
        var suggestedFixes = string.IsNullOrWhiteSpace(Suggestion) ||
                             replacementSpan.IsEmpty ||
                             replacementSpan.Length != ColumnName?.Length
            ? null
            : new[]
            {
                DiagnosticAction.QuickFix(
                    $"Replace '{ColumnName}' with '{Suggestion}'",
                    replacementSpan,
                    Suggestion)
            };

        return SemanticDiagnosticFactory.Create(Code, Message, Span, sourceText, arguments, suggestedFixes);
    }

    private static string[] NormalizeCandidates(IEnumerable<string> candidates)
    {
        var canonicalCandidates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (!canonicalCandidates.TryGetValue(candidate, out var existing) ||
                string.CompareOrdinal(candidate, existing) < 0)
                canonicalCandidates[candidate] = candidate;
        }

        return canonicalCandidates.Values
            .OrderBy(static candidate => candidate, StringComparer.Ordinal)
            .ToArray();
    }

    private static TextSpan GetIdentifierSpan(SourceText? sourceText, TextSpan nodeSpan, string? identifier)
    {
        if (sourceText == null || string.IsNullOrWhiteSpace(identifier) || nodeSpan.IsEmpty)
            return nodeSpan;

        var start = Math.Max(0, nodeSpan.Start);
        var end = Math.Min(sourceText.Length, nodeSpan.End);
        if (start >= end)
            return nodeSpan;

        var index = sourceText.Text.IndexOf(
            identifier,
            start,
            end - start,
            StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? new TextSpan(index, identifier.Length) : nodeSpan;
    }
}
