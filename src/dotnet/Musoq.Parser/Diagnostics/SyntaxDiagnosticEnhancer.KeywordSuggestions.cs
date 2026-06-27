using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static partial class SyntaxDiagnosticEnhancer
{
    private static readonly string[] DialectKeywords = ["LIMIT", "OFFSET", "TOP", "FIRST", "FETCH", "ROWS", "NEXT", "ONLY", "ILIKE"];

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

        foreach (var keyword in DialectKeywords)
        {
            if (!ContainsWholeWord(sourceText.Text, keyword))
                continue;

            return DialectKeywordHelpMap.TryGetValue(keyword, out var help) ? help : null;
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
}
