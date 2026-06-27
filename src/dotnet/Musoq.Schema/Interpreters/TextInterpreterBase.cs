using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Abstract base class for text data interpreters.
///     Generated interpreter classes inherit from this class.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public abstract partial class TextInterpreterBase<TOut> : ITextInterpreter<TOut>
{
    /// <summary>
    ///     Static cache for compiled regex patterns to avoid repeated compilation overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    /// <summary>
    ///     Current parse position in the character sequence during parsing.
    /// </summary>
    protected int ParsePosition { get; set; }

    /// <summary>
    ///     Gets the current parse position. Used when nesting interpreters.
    /// </summary>
    public int Position => ParsePosition;

    /// <inheritdoc />
    public abstract string SchemaName { get; }

    /// <inheritdoc />
    public int CharsConsumed => ParsePosition;

    /// <inheritdoc />
    public TOut Parse(ReadOnlySpan<char> text)
    {
        return ParseAt(text, 0);
    }

    /// <inheritdoc />
    public abstract TOut ParseAt(ReadOnlySpan<char> text, int position);

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

    /// <summary>
    ///     Applies string modifiers to a captured value.
    /// </summary>
    protected static string ApplyModifiers(string value, bool ltrim = false, bool rtrim = false, bool lower = false,
        bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(value);
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

}
