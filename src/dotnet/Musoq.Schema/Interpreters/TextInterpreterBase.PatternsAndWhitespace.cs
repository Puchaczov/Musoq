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
    protected string ReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false, string? fieldName = null,
        bool greedy = false, bool lazy = false, bool ltrim = false, bool rtrim = false, bool lower = false,
        bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var match = ExecutePatternMatch(text, pattern, fieldName, greedy, lazy);
        var result = match.Value;
        ParsePosition += result.Length;
        return ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper);
    }

    /// <summary>
    ///     Reads text matching a regex pattern and returns the Match object for capture group access.
    /// </summary>
    protected Match ReadPatternMatch(ReadOnlySpan<char> text, string pattern, string? fieldName = null,
        bool greedy = false, bool lazy = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var match = ExecutePatternMatch(text, pattern, fieldName, greedy, lazy);
        ParsePosition += match.Value.Length;
        return match;
    }

    private Match ExecutePatternMatch(ReadOnlySpan<char> text, string pattern, string? fieldName, bool greedy,
        bool lazy)
    {
        var remaining = GetTextRemaining(text, fieldName).ToString();
        if (greedy && lazy)
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                "Pattern fields cannot be both greedy and lazy");

        var adjustedPattern = ApplyPatternGreediness(pattern, greedy, lazy);
        var anchoredPattern = adjustedPattern.StartsWith(@"\G", StringComparison.Ordinal)
            ? "^" + adjustedPattern[2..]
            : "^" + adjustedPattern;
        Match match;
        try
        {
            var validationPattern = adjustedPattern.StartsWith(@"\G", StringComparison.Ordinal)
                ? adjustedPattern[2..]
                : adjustedPattern;
            _ = new Regex(
                validationPattern,
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

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
                $"Pattern '{pattern}' is invalid: {exception.Message}",
                exception);
        }
        catch (NotSupportedException exception)
        {
            throw new ParseException(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Pattern '{pattern}' uses an unsupported construct: {exception.Message}",
                exception);
        }

        if (!match.Success)
            throw new ParseException(
                ParseErrorCode.PatternMismatch,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Pattern '{pattern}' did not match at parse position {ParsePosition}");

        return match;
    }

    /// <summary>
    ///     Skips whitespace characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipWhitespace(ReadOnlySpan<char> text, bool required = false, string? fieldName = null)
    {
        EnsureValidParsePosition(text, fieldName);
        var startPos = ParsePosition;
        while (ParsePosition < text.Length && IsTextWhitespace(text[ParsePosition])) ParsePosition++;

        if (required && ParsePosition == startPos)
            throw new ParseException(
                ParseErrorCode.ExpectedWhitespace,
                SchemaName,
                fieldName,
                ParsePosition,
                "Expected whitespace");
    }

    /// <summary>
    ///     Skips at most one whitespace character (for whitespace? quantifier).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SkipOptionalWhitespace(ReadOnlySpan<char> text, string? fieldName = null)
    {
        EnsureValidParsePosition(text, fieldName);
        if (ParsePosition < text.Length && IsTextWhitespace(text[ParsePosition])) ParsePosition++;
    }

    /// <summary>
    ///     Expects and consumes an exact literal string.
    /// </summary>
    protected string ExpectLiteral(ReadOnlySpan<char> text, string literal, string? fieldName = null, bool trim = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(literal);
        var remaining = GetTextRemaining(text, fieldName);
        if (!remaining.StartsWith(literal.AsSpan()))
        {
            var found = remaining.Length >= literal.Length
                ? remaining.Slice(0, literal.Length).ToString()
                : remaining.ToString();
            throw new ParseException(
                ParseErrorCode.LiteralMismatch,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Expected '{literal}', found '{found}'");
        }

        ParsePosition += literal.Length;
        return ApplyModifiers(literal, ltrim || trim, rtrim || trim, lower, upper);
    }
}
