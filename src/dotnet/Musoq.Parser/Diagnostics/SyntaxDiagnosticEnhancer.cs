#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static class SyntaxDiagnosticEnhancer
{
    private static readonly string[] StatementStarts = ["SELECT", "WITH", "DESC", "TABLE", "COUPLE"];

    private static readonly string[] KnownKeywords =
    [
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "TAKE", "SKIP", "WITH",
        "AS", "JOIN", "INNER", "OUTER", "CROSS", "APPLY", "ON", "CASE", "WHEN", "THEN", "ELSE", "END", "IN",
        "NOT", "NULL", "UNION", "EXCEPT", "INTERSECT", "DISTINCT", "TABLE", "COUPLE", "LIKE"
    ];

    private static readonly Dictionary<string, DialectKeywordHelp> DialectKeywordHelpMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["LIMIT"] = new(
                "Musoq uses TAKE instead of LIMIT.",
                [
                    "Replace LIMIT n with TAKE n.",
                    "Example: SELECT Name FROM #schema.method() alias TAKE 5"
                ],
                "Core Spec §TAKE / SKIP"),
            ["OFFSET"] = new(
                "Musoq uses SKIP instead of OFFSET.",
                [
                    "Replace OFFSET n with SKIP n.",
                    "If you need paging, use ORDER BY ... TAKE ... SKIP ..."
                ],
                "Core Spec §TAKE / SKIP"),
            ["TOP"] = new(
                "Musoq does not use TOP in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT TOP 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FIRST"] = new(
                "Musoq does not use FIRST in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT FIRST 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FETCH"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH paging syntax. Use TAKE and SKIP instead.",
                [
                    "Replace OFFSET ... FETCH ... with TAKE ... SKIP ...",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ROWS"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH ROWS syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ROWS/ONLY keywords and rewrite with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["NEXT"] = new(
                "Musoq does not support SQL Server FETCH NEXT syntax. Use TAKE and SKIP instead.",
                [
                    "Rewrite FETCH NEXT n ROWS ONLY as TAKE n.",
                    "Combine with SKIP if you need offset paging."
                ],
                "Core Spec §TAKE / SKIP"),
            ["ONLY"] = new(
                "Musoq does not support SQL Server FETCH ... ONLY syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ONLY and rewrite the paging clause with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ILIKE"] = new(
                "Musoq uses LIKE for pattern matching. ILIKE (case-insensitive LIKE) is a PostgreSQL extension not supported in Musoq.",
                [
                    "Replace ILIKE with LIKE.",
                    "For case-insensitive matching, use: WHERE ToLower(Name) LIKE '%value%'"
                ],
                "Core Spec §LIKE Operator")
        };

    public static Diagnostic CreateDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        Token? currentToken,
        SourceText? sourceText)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var keywordCandidate = GetKeywordCandidate(currentToken, sourceText, span);
        var keywordSuggestion = GetKeywordSuggestion(currentToken, sourceText, span);
        var dialectHelp = GetDialectKeywordHelp(currentToken, sourceText);
        var enhancedMessage = BuildMessage(message, span, keywordCandidate, keywordSuggestion, dialectHelp);
        var explanation = BuildExplanation(metadata, keywordCandidate, keywordSuggestion, dialectHelp);
        var docsReference = dialectHelp?.DocsReference ?? metadata?.DocsReference;
        var suggestedFixes = BuildSuggestedFixes(metadata, keywordCandidate, span, keywordSuggestion, dialectHelp);
        var (location, endLocation) = GetLocations(sourceText, span);
        var contextSnippet = sourceText?.GetContextSnippet(span);

        return new Diagnostic(
            code,
            ErrorCatalog.GetDefaultSeverity(code),
            enhancedMessage,
            location,
            endLocation,
            contextSnippet,
            suggestedFixes: suggestedFixes,
            explanation: explanation,
            docsReference: docsReference);
    }

    public static Diagnostic EnhanceLexerDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        SourceText? sourceText,
        IEnumerable<string>? relatedInfo = null)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var (location, endLocation) = GetLocations(sourceText, span);
        var contextSnippet = sourceText?.GetContextSnippet(span);
        var suggestedFixes = metadata?.SuggestedFixes.Select(DiagnosticAction.Suggestion) ?? [];

        return new Diagnostic(
            code,
            ErrorCatalog.GetDefaultSeverity(code),
            message,
            location,
            endLocation,
            contextSnippet,
            relatedInfo: relatedInfo,
            suggestedFixes: suggestedFixes,
            explanation: metadata?.Explanation,
            docsReference: metadata?.DocsReference);
    }

    private static string BuildMessage(
        string message,
        TextSpan span,
        string? keywordCandidate,
        string? keywordSuggestion,
        DialectKeywordHelp? dialectHelp)
    {
        if (dialectHelp != null)
            return AppendSentence(message, dialectHelp.Message);

        if (!string.IsNullOrWhiteSpace(keywordSuggestion) && !string.IsNullOrWhiteSpace(keywordCandidate) &&
            !string.Equals(keywordCandidate, keywordSuggestion, StringComparison.OrdinalIgnoreCase))
            return AppendSentence(message, $"Did you mean '{keywordSuggestion}'?");

        if (span.Start == 0)
            return AppendSentence(message,
                $"Queries usually start with {string.Join(", ", StatementStarts[..^1])}, or {StatementStarts[^1]}.");

        return message;
    }

    private static string? BuildExplanation(
        ErrorMetadata? metadata,
        string? keywordCandidate,
        string? keywordSuggestion,
        DialectKeywordHelp? dialectHelp)
    {
        if (dialectHelp != null)
            return dialectHelp.Message;

        if (!string.IsNullOrWhiteSpace(keywordSuggestion) && !string.IsNullOrWhiteSpace(keywordCandidate))
            return $"'{keywordCandidate}' is not recognized here. The query likely contains a mistyped Musoq keyword.";

        return metadata?.Explanation;
    }

    private static IReadOnlyList<DiagnosticAction> BuildSuggestedFixes(
        ErrorMetadata? metadata,
        string? keywordCandidate,
        TextSpan span,
        string? keywordSuggestion,
        DialectKeywordHelp? dialectHelp)
    {
        var fixes = new List<DiagnosticAction>();

        if (dialectHelp != null)
            fixes.AddRange(dialectHelp.SuggestedFixes.Select(DiagnosticAction.Suggestion));

        if (!string.IsNullOrWhiteSpace(keywordSuggestion) && !string.IsNullOrWhiteSpace(keywordCandidate) &&
            span.Length > 0 && !string.Equals(keywordCandidate, keywordSuggestion, StringComparison.OrdinalIgnoreCase))
            fixes.Add(DiagnosticAction.QuickFix(
                $"Replace '{keywordCandidate}' with '{keywordSuggestion}'",
                span,
                keywordSuggestion));

        if (metadata?.SuggestedFixes != null)
        {
            foreach (var fix in metadata.SuggestedFixes)
            {
                if (fixes.Any(existing => string.Equals(existing.Title, fix, StringComparison.OrdinalIgnoreCase)))
                    continue;

                fixes.Add(DiagnosticAction.Suggestion(fix));
            }
        }

        return fixes;
    }

    private static (SourceLocation Start, SourceLocation End) GetLocations(SourceText? sourceText, TextSpan span)
    {
        if (sourceText == null)
            return (new SourceLocation(span.Start, 1, span.Start + 1), new SourceLocation(span.End, 1, span.End + 1));

        return sourceText.GetLocations(span);
    }

    private static string? GetKeywordSuggestion(Token? currentToken, SourceText? sourceText, TextSpan span)
    {
        var candidate = GetKeywordCandidate(currentToken, sourceText, span);
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        if (DialectKeywordHelpMap.ContainsKey(candidate))
            return null;

        return ErrorCatalog.GetDidYouMeanSuggestion(candidate, KnownKeywords, maxDistance: 2);
    }

    private static DialectKeywordHelp? GetDialectKeywordHelp(Token? currentToken, SourceText? sourceText)
    {
        if (currentToken != null)
        {
            var directMatch = DialectKeywordHelpMap.GetValueOrDefault(currentToken.Value);
            if (directMatch != null)
                return directMatch;
        }

        if (sourceText == null)
            return null;

        foreach (var keyword in DialectKeywordHelpMap.Keys)
        {
            if (!ContainsWholeWord(sourceText.Text, keyword))
                continue;

            return DialectKeywordHelpMap[keyword];
        }

        return null;
    }

    private static string? GetKeywordCandidate(Token? currentToken, SourceText? sourceText, TextSpan span)
    {
        if (span.Start == 0 && sourceText != null)
        {
            var firstWord = GetFirstWord(sourceText.Text);
            if (!string.IsNullOrWhiteSpace(firstWord))
                return firstWord;
        }

        if (currentToken != null && !string.IsNullOrWhiteSpace(currentToken.Value))
            return currentToken.Value;

        if (sourceText != null && span is { Start: > 0, Length: > 0 })
        {
            var word = ExtractWordAt(sourceText.Text, span);
            if (!string.IsNullOrWhiteSpace(word))
                return word;
        }

        return null;
    }

    private static string? ExtractWordAt(string text, TextSpan span)
    {
        var end = Math.Min(span.Start + span.Length, text.Length);
        if (end <= span.Start)
            return null;

        // Only extract if the span sits on a word boundary (whitespace/start/end on both sides)
        if (span.Start > 0 && !char.IsWhiteSpace(text[span.Start - 1]))
            return null;

        if (end < text.Length && !char.IsWhiteSpace(text[end]))
            return null;

        var word = text[span.Start..end].Trim();
        if (string.IsNullOrWhiteSpace(word))
            return null;

        foreach (var ch in word)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
                return null;
        }

        return word;
    }

    private static string? GetFirstWord(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = 0;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
            start++;

        if (start >= text.Length)
            return null;

        var end = start;
        while (end < text.Length && (char.IsLetter(text[end]) || text[end] == '_'))
            end++;

        return end > start ? text[start..end] : null;
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        var index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var startsAtBoundary = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var endIndex = index + word.Length;
            var endsAtBoundary = endIndex >= text.Length || !char.IsLetterOrDigit(text[endIndex]);

            if (startsAtBoundary && endsAtBoundary)
                return true;

            index = text.IndexOf(word, endIndex, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string AppendSentence(string message, string sentence)
    {
        if (string.IsNullOrWhiteSpace(message))
            return sentence;

        if (message.Contains(sentence, StringComparison.Ordinal))
            return message;

        return message.EndsWith('.') ? $"{message} {sentence}" : $"{message}. {sentence}";
    }

    private sealed record DialectKeywordHelp(string Message, string[] SuggestedFixes, string DocsReference);
}
