namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class TextInterpreterBase<TOut>
{
    protected string ReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
        bool consumeDelimiter = true)
    {
        ArgumentNullException.ThrowIfNull(delimiter);
        var remaining = text.Slice(ParsePosition);
        var index = remaining.IndexOf(delimiter.AsSpan());

        if (index < 0)
            throw new ParseException(
                ParseErrorCode.DelimiterNotFound,
                SchemaName,
                null,
                ParsePosition,
                $"Delimiter '{delimiter}' not found");

        var result = remaining.Slice(0, index).ToString();
        ParsePosition += index + (consumeDelimiter ? delimiter.Length : 0);

        return trim ? result.Trim() : result;
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
        bool trim = false, bool escaped = false)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(close);
        var remaining = text.Slice(ParsePosition);


        if (!remaining.StartsWith(open.AsSpan()))
            throw new ParseException(
                ParseErrorCode.ExpectedDelimiter,
                SchemaName,
                null,
                ParsePosition,
                $"Expected opening delimiter '{open}'");

        ParsePosition += open.Length;
        remaining = text.Slice(ParsePosition);

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
                ParsePosition,
                $"Closing delimiter '{close}' not found");

        var result = remaining.Slice(0, endIndex).ToString();
        ParsePosition += endIndex + close.Length;

        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads exactly N characters.
    /// </summary>
    protected string ReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
        bool rtrim = false)
    {
        if (count < 0)
            throw new ParseException(ParseErrorCode.InvalidSize, SchemaName, null, ParsePosition,
                $"Negative character count: {count}");

        EnsureChars(text, count);
        var result = text.Slice(ParsePosition, count).ToString();
        ParsePosition += count;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim);
    }

    /// <summary>
    ///     Reads a whitespace-delimited token.
    /// </summary>
    protected string ReadToken(ReadOnlySpan<char> text, bool trim = false)
    {
        var remaining = text.Slice(ParsePosition);
        var length = 0;

        while (length < remaining.Length && !char.IsWhiteSpace(remaining[length])) length++;

        var result = remaining.Slice(0, length).ToString();
        ParsePosition += length;

        return trim ? result.Trim() : result;
    }

    /// <summary>
    ///     Reads all remaining text.
    /// </summary>
    protected string ReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false)
    {
        var result = text.Slice(ParsePosition).ToString();
        ParsePosition = text.Length;

        return ApplyModifiers(result, ltrim || trim, rtrim || trim);
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
