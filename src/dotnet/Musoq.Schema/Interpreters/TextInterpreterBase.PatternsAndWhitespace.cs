using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class TextInterpreterBase<TOut>
{
    protected string ReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var match = ExecutePatternMatch(text, pattern);
        var result = match.Value;
        ParsePosition += result.Length;
        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads text matching a regex pattern and returns the Match object for capture group access.
    /// </summary>
    protected Match ReadPatternMatch(ReadOnlySpan<char> text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var match = ExecutePatternMatch(text, pattern);
        ParsePosition += match.Value.Length;
        return match;
    }

    private Match ExecutePatternMatch(ReadOnlySpan<char> text, string pattern)
    {
        var remaining = text.Slice(ParsePosition).ToString();
        var anchoredPattern = pattern.StartsWith(@"\G", StringComparison.Ordinal) ? pattern : @"\G" + pattern;
        var regex = RegexCache.GetOrAdd(anchoredPattern, p => new Regex(p, RegexOptions.Compiled));
        var match = regex.Match(remaining);

        if (!match.Success)
            throw new ParseException(
                ParseErrorCode.PatternMismatch,
                SchemaName,
                null,
                ParsePosition,
                $"Pattern '{pattern}' did not match at parse position {ParsePosition}");

        return match;
    }

    /// <summary>
    ///     Skips whitespace characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipWhitespace(ReadOnlySpan<char> text, bool required = false)
    {
        var startPos = ParsePosition;
        while (ParsePosition < text.Length && char.IsWhiteSpace(text[ParsePosition])) ParsePosition++;

        if (required && ParsePosition == startPos)
            throw new ParseException(
                ParseErrorCode.ExpectedWhitespace,
                SchemaName,
                null,
                ParsePosition,
                "Expected whitespace");
    }

    /// <summary>
    ///     Skips at most one whitespace character (for whitespace? quantifier).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipOptionalWhitespace(ReadOnlySpan<char> text)
    {
        if (ParsePosition < text.Length && char.IsWhiteSpace(text[ParsePosition])) ParsePosition++;
    }

    /// <summary>
    ///     Expects and consumes an exact literal string.
    /// </summary>
    protected void ExpectLiteral(ReadOnlySpan<char> text, string literal)
    {
        ArgumentNullException.ThrowIfNull(literal);
        var remaining = text.Slice(ParsePosition);
        if (!remaining.StartsWith(literal.AsSpan()))
        {
            var found = remaining.Length >= literal.Length
                ? remaining.Slice(0, literal.Length).ToString()
                : remaining.ToString();
            throw new ParseException(
                ParseErrorCode.LiteralMismatch,
                SchemaName,
                null,
                ParsePosition,
                $"Expected '{literal}', found '{found}'");
        }

        ParsePosition += literal.Length;
    }
}
