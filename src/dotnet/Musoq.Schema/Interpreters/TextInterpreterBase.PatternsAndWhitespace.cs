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
    protected ParseAttempt<string> ReadPatternStatus(ReadOnlySpan<char> text, string pattern, bool trim = false, string? fieldName = null,
        bool greedy = false, bool lazy = false, bool ltrim = false, bool rtrim = false, bool lower = false,
        bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var attempt = ExecutePatternMatchStatus(text, pattern, fieldName, greedy, lazy);
        if (attempt.Failure is { } failure)
            return ParseAttempt<string>.Failed(default, failure);
        var match = attempt.Value!;
        var result = match.Value;
        ParsePosition += result.Length;
        return ParseAttempt<string>.Success(ApplyModifiers(result, ltrim || trim, rtrim || trim, lower, upper));
    }

    /// <summary>
    ///     Reads text matching a regex pattern and returns the Match object for capture group access.
    /// </summary>
    protected ParseAttempt<Match> ReadPatternMatchStatus(ReadOnlySpan<char> text, string pattern, string? fieldName = null,
        bool greedy = false, bool lazy = false)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var attempt = ExecutePatternMatchStatus(text, pattern, fieldName, greedy, lazy);
        if (attempt.Failure is not null)
            return attempt;
        var match = attempt.Value!;
        ParsePosition += match.Value.Length;
        return attempt;
    }

    private ParseAttempt<Match> ExecutePatternMatchStatus(ReadOnlySpan<char> text, string pattern, string? fieldName, bool greedy,
        bool lazy)
    {
        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return ParseAttempt<Match>.Failed(default, position);
        var remaining = text[ParsePosition..].ToString();
        if (greedy && lazy)
            return FailText<Match>(ParseErrorCode.GeneralError, fieldName, "Pattern fields cannot be both greedy and lazy");

        var validationPattern = TextPatternCompiler.Normalize(TextPatternCompiler.ApplyGreediness(pattern, greedy, lazy));
        var anchoredPattern = "^" + validationPattern;
        Match match;
        try
        {
            var regex = RegexCache.GetOrAdd(
                anchoredPattern,
                static (key, validation) => TextPatternCompiler.Compile(key, validation),
                validationPattern);
            match = regex.Match(remaining);
        }
        catch (ArgumentException exception)
        {
            return ParseAttempt<Match>.Failed(default, ParseFailure.ForText(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Pattern '{pattern}' is invalid: {exception.Message}",
                exception));
        }
        catch (NotSupportedException exception)
        {
            return ParseAttempt<Match>.Failed(default, ParseFailure.ForText(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"Pattern '{pattern}' uses an unsupported construct: {exception.Message}",
                exception));
        }

        if (!match.Success)
            return FailText<Match>(ParseErrorCode.PatternMismatch, fieldName,
                $"Pattern '{pattern}' did not match at parse position {ParsePosition}");

        return ParseAttempt<Match>.Success(match);
    }

    /// <summary>
    ///     Skips whitespace characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ParseFailure? SkipWhitespaceStatus(ReadOnlySpan<char> text, bool required = false, string? fieldName = null)
    {
        if (CheckTextPositionStatus(text, fieldName) is { } failure)
            return failure;
        var startPos = ParsePosition;
        while (ParsePosition < text.Length && IsTextWhitespace(text[ParsePosition])) ParsePosition++;

        if (required && ParsePosition == startPos)
            return ParseFailure.ForText(
                ParseErrorCode.ExpectedWhitespace,
                SchemaName,
                fieldName,
                ParsePosition,
                "Expected whitespace");
        return null;
    }

    /// <summary>
    ///     Skips at most one whitespace character (for whitespace? quantifier).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected ParseFailure? SkipOptionalWhitespaceStatus(ReadOnlySpan<char> text, string? fieldName = null)
    {
        if (CheckTextPositionStatus(text, fieldName) is { } failure)
            return failure;
        if (ParsePosition < text.Length && IsTextWhitespace(text[ParsePosition])) ParsePosition++;
        return null;
    }

    /// <summary>
    ///     Expects and consumes an exact literal string.
    /// </summary>
    protected ParseAttempt<string> ExpectLiteralStatus(ref TextParseInput input, string literal, string? fieldName = null, bool trim = false,
        bool ltrim = false, bool rtrim = false, bool lower = false, bool upper = false)
    {
        ArgumentNullException.ThrowIfNull(literal);
        var text = input.Text;
        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return ParseAttempt<string>.Failed(default, position);
        var remaining = text[ParsePosition..];
        if (!remaining.StartsWith(literal.AsSpan()))
        {
            if (input.SuppressFailureDetails)
                return ParseAttempt<string>.Failed(default, ParseFailure.TolerantLiteralMismatch);
            var found = remaining[..Math.Min(remaining.Length, literal.Length)];
            return FailText<string>(ParseErrorCode.LiteralMismatch, fieldName, $"Expected '{literal}', found '{found}'");
        }

        ParsePosition += literal.Length;
        return ParseAttempt<string>.Success(ApplyModifiers(literal, ltrim || trim, rtrim || trim, lower, upper));
    }
}
