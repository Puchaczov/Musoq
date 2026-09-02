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
    protected void EnsureValidParsePosition(ReadOnlySpan<char> text, string? fieldName = null)
    {
        if ((uint)ParsePosition <= (uint)text.Length)
            return;

        var reason = ParsePosition < 0 ? "negative" : "past the end of the input";
        throw new ParseException(
            ParseErrorCode.InvalidPosition,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Parse position {ParsePosition} is {reason}; valid range is 0 through {text.Length}");
    }

    protected ReadOnlySpan<char> GetTextRemaining(ReadOnlySpan<char> text, string? fieldName = null)
    {
        EnsureValidParsePosition(text, fieldName);
        return text[ParsePosition..];
    }

    protected void EnsureChars(ReadOnlySpan<char> text, int count, string? fieldName = null)
    {
        if (count < 0)
            throw new ParseException(
                ParseErrorCode.InvalidSize,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Negative character count: {count}");

        EnsureValidParsePosition(text, fieldName);
        if (count > text.Length - ParsePosition)
            ThrowInsufficientData(count, text.Length, fieldName);
    }

    private void ThrowInsufficientData(int count, int dataLength, string? fieldName)
    {
        throw new ParseException(
            ParseErrorCode.InsufficientData,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Attempted to read {count} characters at parse position {ParsePosition}, but only {Math.Max(0, dataLength - ParsePosition)} characters available");
    }

    private static int FindBalancedClose(ReadOnlySpan<char> text, string open, string close)
    {
        if (string.Equals(open, close, StringComparison.Ordinal))
            return text.IndexOf(close.AsSpan());

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

    private static int FindUnescapedClose(ReadOnlySpan<char> text, string close, char escapeCharacter)
    {
        var pos = 0;

        while (pos < text.Length)
        {
            var remaining = text.Slice(pos);
            var idx = remaining.IndexOf(close.AsSpan());
            if (idx < 0) return -1;


            var absPos = pos + idx;
            if (absPos > 0 && text[absPos - 1] == escapeCharacter)
            {
                var backslashCount = 0;
                var check = absPos - 1;
                while (check >= 0 && text[check] == escapeCharacter)
                {
                    backslashCount++;
                    check--;
                }


                if (backslashCount % 2 != 0)
                {
                    pos = absPos + 1;
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
        var remaining = GetTextRemaining(text);
        if (expected.Length > remaining.Length)
            return false;

        return remaining[..expected.Length].SequenceEqual(expected.AsSpan());
    }

    /// <summary>
    ///     Checks if the text at the current position matches the specified regex pattern.
    ///     Does not consume the matched text.
    /// </summary>
    protected bool LookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern, string? fieldName = null)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var remaining = GetTextRemaining(text, fieldName).ToString();


        var normalizedPattern = pattern.StartsWith(@"\G", StringComparison.Ordinal)
            ? pattern[2..]
            : pattern;
        var anchoredPattern = normalizedPattern.StartsWith('^') ? normalizedPattern : "^" + normalizedPattern;
        Match match;
        try
        {
            var regex = RegexCache.GetOrAdd(
                anchoredPattern,
                p => new Regex(
                    p,
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking));
            match = regex.Match(remaining);
        }
        catch (ArgumentException exception)
        {
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Switch lookahead pattern '{pattern}' is invalid: {exception.Message}",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Switch lookahead pattern '{pattern}' uses an unsupported construct: {exception.Message}",
                exception);
        }

        return match is { Success: true, Index: 0 };
    }
}
