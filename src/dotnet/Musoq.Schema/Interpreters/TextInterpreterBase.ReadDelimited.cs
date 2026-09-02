using System.Text;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class TextInterpreterBase<TOut>
{
    protected string ReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
        bool consumeDelimiter = true, string? fieldName = null, bool greedy = false, bool lazy = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(delimiter);
        if (delimiter.Length == 0)
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                "Until fields require a non-empty delimiter");

        var remaining = GetTextRemaining(text, fieldName);
        if (greedy && lazy)
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                "Until fields cannot be both greedy and lazy");

        var index = greedy
            ? remaining.LastIndexOf(delimiter.AsSpan())
            : remaining.IndexOf(delimiter.AsSpan());

        if (index < 0)
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Delimiter '{delimiter}' not found");

        var result = remaining.Slice(0, index).ToString();
        ParsePosition += index + (consumeDelimiter ? delimiter.Length : 0);

        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    /// <summary>
    ///     Reads characters between opening and closing delimiters.
    ///     Both delimiters are consumed but not included in the result.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="open">The opening delimiter.</param>
    /// <param name="close">The closing delimiter.</param>
    /// <param name="nested">
    ///     If true, handle nested open/close pairs (e.g., matching braces). Mutually exclusive with
    ///     <paramref name="escaped" />.
    /// </param>
    /// <param name="trim">If true, trim whitespace from the result.</param>
    /// <param name="escaped">
    ///     If true, ignore close delimiters preceded by backslash. Mutually exclusive with
    ///     <paramref name="nested" />.
    /// </param>
    /// <returns>The text between the delimiters.</returns>
    protected string ReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
        bool trim = false, bool escaped = false, string? escapeCharacter = null, string? fieldName = null,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(close);
        if (open.Length == 0 || close.Length == 0)
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                "Between fields require non-empty opening and closing delimiters");

        var remaining = GetTextRemaining(text, fieldName);


        if (!remaining.StartsWith(open.AsSpan()))
            throw new ParseException(
                ParseErrorCode.ExpectedDelimiter,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Expected opening delimiter '{open}'");

        ParsePosition += open.Length;
        remaining = GetTextRemaining(text, fieldName);

        int endIndex;
        char? escape = null;
        if (escaped)
        {
            if (escapeCharacter is { Length: not 1 })
                throw new ParseException(
                    ParseErrorCode.GeneralError,
                    SchemaName,
                    fieldName,
                    ParsePosition,
                    "An escaped between field requires a one-character escape character.");

            escape = escapeCharacter is null ? '\\' : escapeCharacter[0];
            endIndex = FindUnescapedClose(remaining, close, escape.Value);
        }
        else if (nested)
            endIndex = FindBalancedClose(remaining, open, close);
        else
            endIndex = remaining.IndexOf(close.AsSpan());

        if (endIndex < 0)
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Closing delimiter '{close}' not found");

        var result = remaining.Slice(0, endIndex).ToString();
        if (escape.HasValue)
            result = UnescapeEscapedEscapeCharacters(result, escape.Value);

        ParsePosition += endIndex + close.Length;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    private static string UnescapeEscapedEscapeCharacters(string value, char escapeCharacter)
    {
        var hasDoubledEscape = false;
        for (var i = 0; i + 1 < value.Length; i++)
            if (value[i] == escapeCharacter && value[i + 1] == escapeCharacter)
            {
                hasDoubledEscape = true;
                break;
            }

        if (!hasDoubledEscape)
            return value;

        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            result.Append(value[i]);
            if (value[i] == escapeCharacter && i + 1 < value.Length && value[i + 1] == escapeCharacter)
                i++;
        }

        return result.ToString();
    }

    /// <summary>
    ///     Reads exactly N characters.
    /// </summary>
    protected string ReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
        bool rtrim = false, bool lower = false, bool upper = false, string? fieldName = null)
    {
        if (count < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, fieldName, ParsePosition,
                $"Negative character count: {count}");

        EnsureChars(text, count, fieldName);
        var result = text.Slice(ParsePosition, count).ToString();
        ParsePosition += count;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    /// <summary>
    ///     Reads a whitespace-delimited token.
    /// </summary>
    protected string ReadToken(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false,
        bool lower = false, bool upper = false, string? fieldName = null)
    {
        var remaining = GetTextRemaining(text, fieldName);
        var length = 0;

        while (length < remaining.Length && !IsTextWhitespace(remaining[length])) length++;

        var result = remaining.Slice(0, length).ToString();
        ParsePosition += length;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    /// <summary>
    ///     Reads all remaining text.
    /// </summary>
    protected string ReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false,
        bool lower = false, bool upper = false, string? fieldName = null)
    {
        var result = GetTextRemaining(text, fieldName).ToString();
        ParsePosition = text.Length;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    /// <summary>
    ///     Reads text matching a regex pattern at the current parse position.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="trim">Whether to trim the result.</param>
    /// <returns>The matched text.</returns>
    /// <exception cref="ParseException">Thrown if the pattern does not match at the current parse position.</exception>
}
