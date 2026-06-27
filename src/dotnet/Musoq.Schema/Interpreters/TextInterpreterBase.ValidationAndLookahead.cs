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
    protected void EnsureChars(ReadOnlySpan<char> text, int count)
    {
        if (ParsePosition + count > text.Length)
            ThrowInsufficientData(count, text.Length);
    }

    private void ThrowInsufficientData(int count, int dataLength)
    {
        throw new ParseException(
            ParseErrorCode.InsufficientData,
            SchemaName,
            null,
            ParsePosition,
            $"Attempted to read {count} characters at parse position {ParsePosition}, but only {dataLength - ParsePosition} characters available");
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
                ParsePosition,
                message);
    }

    /// <summary>
    ///     Checks if the current position is at or past the end of the text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsAtEnd(ReadOnlySpan<char> text)
    {
        return ParsePosition >= text.Length;
    }

    /// <summary>
    ///     Checks if the text at the current position matches the specified string.
    ///     Does not consume the matched text.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool LookaheadMatches(ReadOnlySpan<char> text, string expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (ParsePosition + expected.Length > text.Length)
            return false;

        return text.Slice(ParsePosition, expected.Length).SequenceEqual(expected.AsSpan());
    }

    /// <summary>
    ///     Checks if the text at the current position matches the specified regex pattern.
    ///     Does not consume the matched text.
    /// </summary>
    protected bool LookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var remaining = text.Slice(ParsePosition).ToString();


        var anchoredPattern = pattern.StartsWith('^') ? pattern : "^" + pattern;
        var regex = RegexCache.GetOrAdd(anchoredPattern, p => new Regex(p, RegexOptions.Compiled));
        var match = regex.Match(remaining);

        return match is { Success: true, Index: 0 };
    }
}
