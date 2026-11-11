using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>Retains the established throwing captured-whitespace reader.</summary>
    protected string ReadWhitespace(ReadOnlySpan<char> text, string quantifier = "+", bool trim = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false, string? fieldName = null) =>
        ReadWhitespaceStatus(text, quantifier, trim, ltrim, rtrim, lower, upper, fieldName).GetValueOrThrow();
    /// <summary>Retains the established throwing whitespace reader.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected void SkipWhitespace(ReadOnlySpan<char> text, bool required = false, string? fieldName = null)
    {
        if (SkipWhitespaceStatus(text, required, fieldName) is { } failure)
            throw failure.ToException();
    }

    /// <summary>Retains the established throwing optional whitespace reader.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    protected void SkipOptionalWhitespace(ReadOnlySpan<char> text, string? fieldName = null)
    {
        if (SkipOptionalWhitespaceStatus(text, fieldName) is { } failure)
            throw failure.ToException();
    }

    /// <summary>Retains the established throwing delimiter reader for external subclasses.</summary>
    protected string ReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
        bool consumeDelimiter = true, string? fieldName = null, bool greedy = false, bool lazy = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false) =>
        ReadUntilStatus(text, delimiter, trim, consumeDelimiter, fieldName, greedy, lazy, ltrim, rtrim, lower, upper).GetValueOrThrow();

    /// <summary>Retains the established throwing balanced/escaped delimiter reader.</summary>
    protected string ReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
        bool trim = false, bool escaped = false, string? escapeCharacter = null, string? fieldName = null,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false) =>
        ReadBetweenStatus(text, open, close, nested, trim, escaped, escapeCharacter, fieldName, ltrim, rtrim, lower, upper).GetValueOrThrow();

    /// <summary>Retains the established throwing fixed-length reader.</summary>
    protected string ReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
        bool rtrim = false, bool lower = false, bool upper = false, string? fieldName = null) =>
        ReadCharsStatus(text, count, trim, ltrim, rtrim, lower, upper, fieldName).GetValueOrThrow();

    /// <summary>Retains the established throwing token reader.</summary>
    protected string ReadToken(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false,
        bool lower = false, bool upper = false, string? fieldName = null) =>
        ReadTokenStatus(text, trim, ltrim, rtrim, lower, upper, fieldName).GetValueOrThrow();

    /// <summary>Retains the established throwing remainder reader.</summary>
    protected string ReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false,
        bool lower = false, bool upper = false, string? fieldName = null) =>
        ReadRestStatus(text, trim, ltrim, rtrim, lower, upper, fieldName).GetValueOrThrow();

    /// <summary>Retains the established throwing pattern reader.</summary>
    protected string ReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false, string? fieldName = null,
        bool greedy = false, bool lazy = false, bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false) =>
        ReadPatternStatus(text, pattern, trim, fieldName, greedy, lazy, ltrim, rtrim, lower, upper).GetValueOrThrow();

    /// <summary>Retains the established throwing capture reader.</summary>
    protected Match ReadPatternMatch(ReadOnlySpan<char> text, string pattern, string? fieldName = null,
        bool greedy = false, bool lazy = false) => ReadPatternMatchStatus(text, pattern, fieldName, greedy, lazy).GetValueOrThrow();

    /// <summary>Retains the established throwing literal reader.</summary>
    protected string ExpectLiteral(ReadOnlySpan<char> text, string literal, string? fieldName = null, bool trim = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false)
    {
        var input = new TextParseInput(text);
        return ExpectLiteralStatus(ref input, literal, fieldName, trim, ltrim, rtrim, lower, upper).GetValueOrThrow();
    }
}
