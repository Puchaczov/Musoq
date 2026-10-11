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
        throw ParseException.ForText(
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
            throw ParseException.ForText(
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
        throw ParseException.ForText(
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
        var position = 0;
        while (position < text.Length)
        {
            var offset = text[position..].IndexOf(close.AsSpan());
            if (offset < 0)
                return -1;

            var candidate = position + offset;
            if (close.Length == 1 && close[0] == escapeCharacter)
            {
                if (candidate + 1 < text.Length && text[candidate + 1] == escapeCharacter)
                {
                    position = candidate + 2;
                    continue;
                }

                return candidate;
            }

            if (HasOddPrecedingEscapes(text, candidate, escapeCharacter))
            {
                position = candidate + 1;
                continue;
            }

            return candidate;
        }

        return -1;
    }

    private static bool HasOddPrecedingEscapes(ReadOnlySpan<char> text, int position, char escapeCharacter)
    {
        var count = 0;
        for (var index = position - 1; index >= 0 && text[index] == escapeCharacter; index--)
            count++;

        return count % 2 != 0;
    }

    private static int EscapedTokenLength(ReadOnlySpan<char> text, string close, char escapeCharacter)
    {
        if (text.Length < 2 || text[0] != escapeCharacter)
            return 0;

        if (text[1] == escapeCharacter)
            return 2;

        if (text[1..].StartsWith(close.AsSpan()))
            return close.Length + 1;

        return 0;
    }

    /// <summary>
    ///     Validates a condition and throws if it fails.
    /// </summary>
    protected void Validate(bool condition, string fieldName, string message)
    {
        if (!condition)
            throw ParseException.ForText(
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
            throw ParseException.ForText(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Switch lookahead pattern '{pattern}' is invalid: {exception.Message}",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw ParseException.ForText(
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
