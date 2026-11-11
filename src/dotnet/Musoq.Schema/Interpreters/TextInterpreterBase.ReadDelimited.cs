using System.Text;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class TextInterpreterBase<TOut>
{
    protected ParseAttempt<string> ReadUntilStatus(
        ReadOnlySpan<char> text,
        string delimiter,
        bool trim = false,
        bool consumeDelimiter = true,
        string? fieldName = null,
        bool greedy = false,
        bool lazy = false,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false
    )
    {
        ArgumentNullException.ThrowIfNull(delimiter);
        if (delimiter.Length == 0)
            return FailText<string>(
                ParseErrorCode.GeneralError,
                fieldName,
                "Until fields require a non-empty delimiter"
            );

        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return ParseAttempt<string>.Failed(default, position);
        var remaining = text[ParsePosition..];
        if (greedy && lazy)
            return FailText<string>(
                ParseErrorCode.GeneralError,
                fieldName,
                "Until fields cannot be both greedy and lazy"
            );

        var index = greedy ? remaining.LastIndexOf(delimiter.AsSpan()) : remaining.IndexOf(delimiter.AsSpan());

        if (index < 0)
            return FailText<string>(ParseErrorCode.DelimiterNotFound, fieldName, $"Delimiter '{delimiter}' not found");

        var result = remaining.Slice(0, index).ToString();
        ParsePosition += index + (consumeDelimiter ? delimiter.Length : 0);

        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
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
    ///     If true, decode escaped closing delimiters and doubled escape characters. Mutually exclusive with
    ///     <paramref name="nested" />.
    /// </param>
    /// <returns>The text between the delimiters.</returns>
    protected ParseAttempt<string> ReadBetweenStatus(
        ReadOnlySpan<char> text,
        string open,
        string close,
        bool nested = false,
        bool trim = false,
        bool escaped = false,
        string? escapeCharacter = null,
        string? fieldName = null,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false
    )
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(close);
        if (open.Length == 0 || close.Length == 0)
            return FailText<string>(
                ParseErrorCode.GeneralError,
                fieldName,
                "Between fields require non-empty opening and closing delimiters"
            );

        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return ParseAttempt<string>.Failed(default, position);
        var remaining = text[ParsePosition..];

        if (!remaining.StartsWith(open.AsSpan()))
            return FailText<string>(
                ParseErrorCode.ExpectedDelimiter,
                fieldName,
                $"Expected opening delimiter '{open}'"
            );

        ParsePosition += open.Length;
        remaining = GetTextRemaining(text, fieldName);

        int endIndex;
        char? escape = null;
        if (escaped)
        {
            if (escapeCharacter is { Length: not 1 })
                return FailText<string>(
                    ParseErrorCode.GeneralError,
                    fieldName,
                    "An escaped between field requires a one-character escape character."
                );

            escape = escapeCharacter is null ? '\\' : escapeCharacter[0];
            endIndex = FindUnescapedClose(remaining, close, escape.Value);
        }
        else if (nested)
            endIndex = FindBalancedClose(remaining, open, close);
        else
            endIndex = remaining.IndexOf(close.AsSpan());

        if (endIndex < 0)
            return FailText<string>(
                ParseErrorCode.DelimiterNotFound,
                fieldName,
                $"Closing delimiter '{close}' not found"
            );

        var result = remaining.Slice(0, endIndex).ToString();
        if (escape.HasValue)
            result = DecodeEscapedContent(result, close, escape.Value);

        ParsePosition += endIndex + close.Length;

        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
    }

    private static string DecodeEscapedContent(string value, string close, char escapeCharacter)
    {
        var result = new StringBuilder(value.Length);
        for (var position = 0; position < value.Length; position++)
        {
            var escapedLength = EscapedTokenLength(value.AsSpan(position), close, escapeCharacter);
            if (escapedLength > 0)
            {
                result.Append(value.AsSpan(position + 1, escapedLength - 1));
                position += escapedLength - 1;
                continue;
            }

            result.Append(value[position]);
        }

        return result.ToString();
    }

    /// <summary>
    ///     Reads exactly N characters.
    /// </summary>
    protected ParseAttempt<string> ReadCharsStatus(
        ReadOnlySpan<char> text,
        int count,
        bool trim = false,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false,
        string? fieldName = null
    )
    {
        if (PrepareCharsStatus(text, count, fieldName) is { } failure)
            return ParseAttempt<string>.Failed(default, failure);
        var result = text.Slice(ParsePosition, count).ToString();
        ParsePosition += count;

        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
    }

    /// <summary>
    ///     Reads a whitespace-delimited token.
    /// </summary>
    protected ParseAttempt<string> ReadTokenStatus(
        ReadOnlySpan<char> text,
        bool trim = false,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false,
        string? fieldName = null
    )
    {
        if (CheckTextPositionStatus(text, fieldName) is { } failure)
            return ParseAttempt<string>.Failed(default, failure);
        var remaining = text[ParsePosition..];
        var length = 0;

        while (length < remaining.Length && !IsTextWhitespace(remaining[length]))
            length++;

        var result = remaining.Slice(0, length).ToString();
        ParsePosition += length;

        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
    }

    /// <summary>
    ///     Reads all remaining text.
    /// </summary>
    protected ParseAttempt<string> ReadRestStatus(
        ReadOnlySpan<char> text,
        bool trim = false,
        bool ltrim = false,
        bool rtrim = false,
        bool lower = false,
        bool upper = false,
        string? fieldName = null
    )
    {
        if (CheckTextPositionStatus(text, fieldName) is { } failure)
            return ParseAttempt<string>.Failed(default, failure);
        var result = text[ParsePosition..].ToString();
        ParsePosition = text.Length;

        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
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
