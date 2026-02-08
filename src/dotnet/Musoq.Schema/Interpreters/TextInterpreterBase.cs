#nullable enable

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract class TextInterpreterBase<TOut> : ITextInterpreter<TOut>
{
    /// <summary>
    ///     Static cache for compiled regex patterns to avoid repeated compilation overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    /// <summary>
    ///     Current _parsePosition in the character sequence during parsing.
    /// </summary>
    protected int _parsePosition;

    /// <summary>
    ///     Gets the current parse position. Used when nesting interpreters.
    /// </summary>
    public int Position => _parsePosition;

    /// <inheritdoc />
    public abstract string SchemaName { get; }

    /// <inheritdoc />
    public int CharsConsumed => _parsePosition;

    /// <inheritdoc />
    public TOut Parse(ReadOnlySpan<char> text)
    {
        return ParseAt(text, 0);
    }

    /// <inheritdoc />
    public abstract TOut ParseAt(ReadOnlySpan<char> text, int _parsePosition);

    /// <summary>
    ///     Parses the specified text string.
    ///     This overload enables easier invocation via reflection for testing.
    /// </summary>
    /// <param name="text">The text string to parse.</param>
    /// <returns>The parsed result object.</returns>
    public TOut Parse(string text)
    {
        return Parse(text.AsSpan());
    }

    /// <inheritdoc />
    public bool TryParse(ReadOnlySpan<char> text, out TOut? result)
    {
        try
        {
            result = Parse(text);
            return true;
        }
        catch (ParseException)
        {
            result = default;
            return false;
        }
    }

    #region Modifier Helpers

    /// <summary>
    ///     Applies string modifiers to a captured value.
    /// </summary>
    protected static string ApplyModifiers(string value, bool ltrim = false, bool rtrim = false, bool lower = false,
        bool upper = false)
    {
        if (!ltrim && !rtrim && !lower && !upper)
            return value;

        if (ltrim && rtrim)
            value = value.Trim();
        else if (ltrim)
            value = value.TrimStart();
        else if (rtrim)
            value = value.TrimEnd();

        if (lower)
            value = value.ToLowerInvariant();
        else if (upper)
            value = value.ToUpperInvariant();

        return value;
    }

    #endregion

    #region Text Reading Helpers

    /// <summary>
    ///     Reads characters until the specified delimiter is found.
    ///     The delimiter is consumed but not included in the result by default.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="delimiter">The delimiter to stop at.</param>
    /// <param name="trim">Whether to trim whitespace from the result.</param>
    /// <param name="consumeDelimiter">
    ///     Whether to consume the delimiter (default true).
    ///     Set to false to leave the delimiter in place for the next read.
    /// </param>
    protected string ReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
        bool consumeDelimiter = true)
    {
        var remaining = text.Slice(_parsePosition);
        var index = remaining.IndexOf(delimiter.AsSpan());

        if (index < 0)
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                SchemaName,
                null,
                _parsePosition,
                $"Delimiter '{delimiter}' not found");

        var result = remaining.Slice(0, index).ToString();
        _parsePosition += index + (consumeDelimiter ? delimiter.Length : 0);

        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads characters between opening and closing delimiters.
    ///     Both delimiters are consumed but not included in the result.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="open">The opening delimiter.</param>
    /// <param name="close">The closing delimiter.</param>
    /// <param name="nested">If true, handle nested open/close pairs (e.g., matching braces). Mutually exclusive with <paramref name="escaped"/>.</param>
    /// <param name="trim">If true, trim whitespace from the result.</param>
    /// <param name="escaped">If true, ignore close delimiters preceded by backslash. Mutually exclusive with <paramref name="nested"/>.</param>
    /// <returns>The text between the delimiters.</returns>
    protected string ReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
        bool trim = false, bool escaped = false)
    {
        var remaining = text.Slice(_parsePosition);


        if (!remaining.StartsWith(open.AsSpan()))
            throw new ParseException(
                ParseErrorCode.ExpectedDelimiter,
                SchemaName,
                null,
                _parsePosition,
                $"Expected opening delimiter '{open}'");

        _parsePosition += open.Length;
        remaining = text.Slice(_parsePosition);

        int endIndex;
        if (escaped)
            endIndex = FindUnescapedClose(remaining, close);
        else if (nested)
            endIndex = FindBalancedClose(remaining, open, close);
        else
            endIndex = remaining.IndexOf(close.AsSpan());

        if (endIndex < 0)
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                SchemaName,
                null,
                _parsePosition,
                $"Closing delimiter '{close}' not found");

        var result = remaining.Slice(0, endIndex).ToString();
        _parsePosition += endIndex + close.Length;

        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads exactly N characters.
    /// </summary>
    protected string ReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
        bool rtrim = false)
    {
        if (count < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, _parsePosition,
                $"Negative character count: {count}");

        EnsureChars(text, count);
        var result = text.Slice(_parsePosition, count).ToString();
        _parsePosition += count;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim);
    }

    /// <summary>
    ///     Reads a whitespace-delimited token.
    /// </summary>
    protected string ReadToken(ReadOnlySpan<char> text, bool trim = false)
    {
        var remaining = text.Slice(_parsePosition);
        var length = 0;

        while (length < remaining.Length && !char.IsWhiteSpace(remaining[length])) length++;

        var result = remaining.Slice(0, length).ToString();
        _parsePosition += length;

        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads all remaining text.
    /// </summary>
    protected string ReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false)
    {
        var result = text.Slice(_parsePosition).ToString();
        _parsePosition = text.Length;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim);
    }

    /// <summary>
    ///     Reads text matching a regex pattern at the current _parsePosition.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="trim">Whether to trim the result.</param>
    /// <returns>The matched text.</returns>
    /// <exception cref="ParseException">Thrown if the pattern does not match at the current _parsePosition.</exception>
    protected string ReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false)
    {
        var match = ExecutePatternMatch(text, pattern);
        var result = match.Value;
        _parsePosition += result.Length;
        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads text matching a regex pattern and returns the Match object for capture group access.
    /// </summary>
    protected System.Text.RegularExpressions.Match ReadPatternMatch(ReadOnlySpan<char> text, string pattern)
    {
        var match = ExecutePatternMatch(text, pattern);
        _parsePosition += match.Value.Length;
        return match;
    }

    private System.Text.RegularExpressions.Match ExecutePatternMatch(ReadOnlySpan<char> text, string pattern)
    {
        var remaining = text.Slice(_parsePosition).ToString();
        var anchoredPattern = pattern.StartsWith(@"\G") ? pattern : @"\G" + pattern;
        var regex = RegexCache.GetOrAdd(anchoredPattern, p => new Regex(p, RegexOptions.Compiled));
        var match = regex.Match(remaining);

        if (!match.Success)
            throw new ParseException(
                ParseErrorCode.PatternMismatch,
                SchemaName,
                null,
                _parsePosition,
                $"Pattern '{pattern}' did not match at _parsePosition {_parsePosition}");

        return match;
    }

    /// <summary>
    ///     Skips whitespace characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipWhitespace(ReadOnlySpan<char> text, bool required = false)
    {
        var startPos = _parsePosition;
        while (_parsePosition < text.Length && char.IsWhiteSpace(text[_parsePosition])) _parsePosition++;

        if (required && _parsePosition == startPos)
            throw new ParseException(
                ParseErrorCode.ExpectedWhitespace,
                SchemaName,
                null,
                _parsePosition,
                "Expected whitespace");
    }

    /// <summary>
    ///     Skips at most one whitespace character (for whitespace? quantifier).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipOptionalWhitespace(ReadOnlySpan<char> text)
    {
        if (_parsePosition < text.Length && char.IsWhiteSpace(text[_parsePosition])) _parsePosition++;
    }

    /// <summary>
    ///     Expects and consumes an exact literal string.
    /// </summary>
    protected void ExpectLiteral(ReadOnlySpan<char> text, string literal)
    {
        var remaining = text.Slice(_parsePosition);
        if (!remaining.StartsWith(literal.AsSpan()))
        {
            var found = remaining.Length >= literal.Length
                ? remaining.Slice(0, literal.Length).ToString()
                : remaining.ToString();
            throw new ParseException(
                ParseErrorCode.LiteralMismatch,
                SchemaName,
                null,
                _parsePosition,
                $"Expected '{literal}', found '{found}'");
        }

        _parsePosition += literal.Length;
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    ///     Ensures sufficient characters are available for reading.
    ///     Uses AggressiveInlining to allow JIT to optimize hot paths.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureChars(ReadOnlySpan<char> text, int count)
    {
        if (_parsePosition + count > text.Length)
            ThrowInsufficientData(count, text.Length);
    }

    private void ThrowInsufficientData(int count, int dataLength)
    {
        throw new ParseException(
            ParseErrorCode.InsufficientData,
            SchemaName,
            null,
            _parsePosition,
            $"Attempted to read {count} characters at _parsePosition {_parsePosition}, but only {dataLength - _parsePosition} characters available");
    }

    private static int FindBalancedClose(ReadOnlySpan<char> text, string open, string close)
    {
        var depth = 1;
        var pos = 0;

        while (pos < text.Length && depth > 0)
        {
            var remaining = text.Slice(pos);

            if (remaining.StartsWith(open.AsSpan()))
            {
                depth++;
                pos += open.Length;
            }
            else if (remaining.StartsWith(close.AsSpan()))
            {
                depth--;
                if (depth == 0)
                    return pos;
                pos += close.Length;
            }
            else
            {
                pos++;
            }
        }

        return -1;
    }

        private static int FindUnescapedClose(ReadOnlySpan<char> text, string close)
    {
        var pos = 0;

        while (pos < text.Length)
        {
            var remaining = text.Slice(pos);
            var idx = remaining.IndexOf(close.AsSpan());
            if (idx < 0) return -1;

            
            var absPos = pos + idx;
            if (absPos > 0 && text[absPos - 1] == '\\')
            {
                
                var backslashCount = 0;
                var check = absPos - 1;
                while (check >= 0 && text[check] == '\\')
                {
                    backslashCount++;
                    check--;
                }

                
                if (backslashCount % 2 != 0)
                {
                    pos = absPos + close.Length;
                    continue;
                }
            }

            return absPos;
        }

        return -1;
    }

    /// <summary>
    ///     Validates a condition and throws if it fails.
    /// </summary>
    protected void Validate(bool condition, string fieldName, string message)
    {
        if (!condition)
            throw new ParseException(
                ParseErrorCode.ValidationFailed,
                SchemaName,
                fieldName,
                _parsePosition,
                message);
    }

    /// <summary>
    ///     Checks if the current position is at or past the end of the text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsAtEnd(ReadOnlySpan<char> text)
    {
        return _parsePosition >= text.Length;
    }

    /// <summary>
    ///     Checks if the text at the current position matches the specified string.
    ///     Does not consume the matched text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool LookaheadMatches(ReadOnlySpan<char> text, string expected)
    {
        if (_parsePosition + expected.Length > text.Length)
            return false;

        return text.Slice(_parsePosition, expected.Length).SequenceEqual(expected.AsSpan());
    }

    /// <summary>
    ///     Checks if the text at the current position matches the specified regex pattern.
    ///     Does not consume the matched text.
    /// </summary>
    protected bool LookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern)
    {
        var remaining = text.Slice(_parsePosition).ToString();


        var anchoredPattern = pattern.StartsWith("^") ? pattern : "^" + pattern;
        var regex = RegexCache.GetOrAdd(anchoredPattern, p => new Regex(p, RegexOptions.Compiled));
        var match = regex.Match(remaining);

        return match.Success && match.Index == 0;
    }

    #endregion
}
