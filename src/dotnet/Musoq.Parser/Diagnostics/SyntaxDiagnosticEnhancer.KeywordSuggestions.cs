using System;
using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static partial class SyntaxDiagnosticEnhancer
{
    private static readonly string[] DialectKeywords = ["LIMIT", "OFFSET", "TOP", "FIRST", "FETCH", "ROWS", "NEXT", "ONLY", "ILIKE", "CAST"];

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

    private static DialectKeywordHelp? GetDialectKeywordHelp(Token? currentToken, SourceText? sourceText, TextSpan span)
    {
        if (sourceText != null && span.Length > 0 && span.Start >= 0 && span.End <= sourceText.Text.Length &&
            IsWholeWordAt(sourceText.Text, span.Start, span.Length))
        {
            var spanText = sourceText.Text.Substring(span.Start, span.Length);
            foreach (var keyword in DialectKeywords)
            {
                if (string.Equals(spanText, keyword, StringComparison.OrdinalIgnoreCase))
                    return DialectKeywordHelpMap.GetValueOrDefault(keyword);
            }
        }

        if (sourceText == null && currentToken != null && currentToken.Span == span)
        {
            var directMatch = DialectKeywordHelpMap.GetValueOrDefault(currentToken.Value);
            if (directMatch != null)
                return directMatch;
        }

        if (sourceText != null)
        {
            if (IsIncompleteWindowRowsFrame(sourceText.Text, span.Start))
                return DialectKeywordHelpMap["ROWS"];

            var nearbyHelp = GetNearbyDialectKeywordHelp(sourceText.Text, span.Start);
            if (nearbyHelp != null)
                return nearbyHelp;
        }

        return IsCastKeywordContext(currentToken, sourceText?.Text, span.Start)
            ? DialectKeywordHelpMap["CAST"]
            : null;
    }

    private static DialectKeywordHelp? GetNearbyDialectKeywordHelp(string text, int diagnosticStart)
    {
        var nearestPosition = -1;
        string? nearestKeyword = null;

        foreach (var keyword in DialectKeywords)
        {
            if (keyword is "CAST" or "TOP" or "FIRST")
                continue;

            var position = FindLastWholeWordBefore(text, keyword, diagnosticStart);
            if (position <= nearestPosition || !IsNearbyDialectKeyword(text, keyword, position, diagnosticStart))
                continue;

            nearestPosition = position;
            nearestKeyword = keyword;
        }

        return nearestKeyword == null ? null : DialectKeywordHelpMap.GetValueOrDefault(nearestKeyword);
    }

    private static bool IsNearbyDialectKeyword(string text, string keyword, int position, int diagnosticStart)
    {
        const int maxDistance = 96;

        if (position < 0 || diagnosticStart <= position || diagnosticStart - position > maxDistance ||
            (position > 0 && !char.IsWhiteSpace(text[position - 1])))
            return false;

        var afterKeyword = position + keyword.Length;
        if (afterKeyword >= text.Length || !char.IsWhiteSpace(text[afterKeyword]))
            return false;

        var nextToken = SkipWhiteSpace(text, afterKeyword);
        return keyword is "LIMIT" or "OFFSET"
            ? nextToken < text.Length && IsNumericOrParameterStart(text[nextToken])
            : keyword == "FETCH"
                ? IsWholeWordAt(text, nextToken, "NEXT".Length) &&
                  string.Equals(text.Substring(nextToken, "NEXT".Length), "NEXT", StringComparison.OrdinalIgnoreCase)
                : keyword is "NEXT" or "ONLY"
                    ? FindLastWholeWordBefore(text, "FETCH", position) >= 0
                    : keyword == "ROWS"
                        ? FindLastWholeWordBefore(text, "FETCH", position) >= 0 ||
                          FindLastWholeWordBefore(text, "OFFSET", position) >= 0
                        : keyword == "ILIKE";
    }

    private static int SkipWhiteSpace(string text, int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
            position++;

        return position;
    }

    private static bool IsNumericOrParameterStart(char character)
    {
        return char.IsDigit(character) || character is '-' or '+' or '$' or '?';
    }

    private static bool IsWholeWordAt(string text, int start, int length)
    {
        if (start < 0 || length <= 0 || start + length > text.Length)
            return false;

        var end = start + length;
        var startsAtBoundary = start == 0 || IsDialectWordBoundary(text[start - 1]);
        var endsAtBoundary = end == text.Length || IsDialectWordBoundary(text[end]);
        return startsAtBoundary && endsAtBoundary;
    }

    private static bool IsDialectWordBoundary(char character)
    {
        return !char.IsLetterOrDigit(character) && character != '_' && character != '.';
    }

    private static bool IsIncompleteWindowRowsFrame(string text, int diagnosticStart)
    {
        if (diagnosticStart <= 0 || diagnosticStart > text.Length)
            return false;

        var rowsPosition = FindLastWholeWordBefore(text, "ROWS", diagnosticStart);
        if (rowsPosition < 0)
            return false;

        var overPosition = FindLastWholeWordBefore(text, "OVER", rowsPosition);
        if (overPosition < 0)
            return false;

        var openingParenthesis = text.IndexOf('(', overPosition + 4, rowsPosition - overPosition - 4);
        if (openingParenthesis < 0 ||
            text.IndexOf(')', openingParenthesis + 1, rowsPosition - openingParenthesis - 1) >= 0)
            return false;

        return text.IndexOf(')', rowsPosition + 4, diagnosticStart - rowsPosition - 4) < 0;
    }

    private static int FindLastWholeWordBefore(string text, string word, int exclusiveEnd)
    {
        var searchEnd = Math.Min(exclusiveEnd, text.Length);
        var searchFrom = 0;
        var lastMatch = -1;

        while (searchFrom < searchEnd)
        {
            var match = text.IndexOf(word, searchFrom, searchEnd - searchFrom, StringComparison.OrdinalIgnoreCase);
            if (match < 0)
                break;

            var endsAt = match + word.Length;
            if (endsAt <= searchEnd && IsWholeWordAt(text, match, word.Length))
                lastMatch = match;

            searchFrom = endsAt;
        }

        return lastMatch;
    }

    private static bool IsCastKeywordContext(Token? currentToken, string? text, int position)
    {
        if (currentToken == null || currentToken.TokenType != TokenType.As || string.IsNullOrEmpty(text) || position <= 0)
            return false;

        var searchFrom = 0;
        while (searchFrom < position)
        {
            var castPosition = text.IndexOf("CAST", searchFrom, position - searchFrom, StringComparison.OrdinalIgnoreCase);
            if (castPosition < 0)
                return false;

            var afterCast = castPosition + 4;
            if ((castPosition == 0 || !char.IsLetterOrDigit(text[castPosition - 1])) &&
                (afterCast >= text.Length || !char.IsLetterOrDigit(text[afterCast])))
            {
                while (afterCast < position && char.IsWhiteSpace(text[afterCast]))
                    afterCast++;

                if (afterCast < position && text[afterCast] == '(')
                    return true;
            }

            searchFrom = afterCast;
        }

        return false;
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
