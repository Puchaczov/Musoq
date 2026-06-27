using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static partial class SyntaxDiagnosticEnhancer
{
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

    private static List<DiagnosticAction> BuildSuggestedFixes(
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
}
