using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>Reads a prepared field directly from the remaining span.</summary>
    protected ParseAttempt<string> ReadPreparedPatternStatus(
        ReadOnlySpan<char> text,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        var attempt = FindPreparedPattern(text, pattern, fieldName);
        if (attempt.Failure is { } failure)
            return ParseAttempt<string>.Failed(default, failure);

        var (index, length) = attempt.Value;
        var value = text.Slice(ParsePosition + index, length).ToString();
        ParsePosition += length;
        return ParseAttempt<string>.Success(ApplyPreparedModifiers(value, pattern.Modifiers));
    }

    /// <summary>Consumes a prepared pattern whose matched text is discarded.</summary>
    protected ParseFailure? SkipPreparedPatternStatus(
        ReadOnlySpan<char> text,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        var attempt = FindPreparedPattern(text, pattern, fieldName);
        if (attempt.Succeeded)
            ParsePosition += attempt.Value.Length;
        return attempt.Failure;
    }

    /// <summary>Matches captures within the current region of a single parse's backing text.</summary>
    protected ParseAttempt<Match> ReadPreparedPatternMatchStatus(
        ref TextParseInput input,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        var prepared = PreparePatternStatus(input.Text, pattern, fieldName);
        if (prepared.Failure is { } failure)
            return ParseAttempt<Match>.Failed(default, failure);

        var match = prepared.Value!.Match(input.CaptureText, ParsePosition, input.Text.Length - ParsePosition);
        if (!match.Success)
            return ParseAttempt<Match>.Failed(default, PatternMismatch(pattern, fieldName));

        ParsePosition += match.Length;
        return ParseAttempt<Match>.Success(match);
    }

    /// <summary>Reads a capture using the mapping resolved for this prepared pattern.</summary>
    protected static string GetPreparedCapture(PreparedTextPattern pattern, Match match, int slot) =>
        ApplyPreparedModifiers(pattern.Capture(match, slot), pattern.Modifiers);

    /// <summary>Tests a prepared switch pattern without materializing text or advancing the cursor.</summary>
    protected bool LookaheadMatchesPatternPrepared(
        ReadOnlySpan<char> text,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        var regex = PreparePatternStatus(text, pattern, fieldName).GetValueOrThrow();
        var matches = regex.EnumerateMatches(text[ParsePosition..]);
        return matches.MoveNext() && matches.Current.Index == 0;
    }

    private ParseAttempt<(int Index, int Length)> FindPreparedPattern(
        ReadOnlySpan<char> text,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        var prepared = PreparePatternStatus(text, pattern, fieldName);
        if (prepared.Failure is { } failure)
            return ParseAttempt<(int, int)>.Failed(default, failure);

        var matches = prepared.Value!.EnumerateMatches(text[ParsePosition..]);
        if (!matches.MoveNext())
            return ParseAttempt<(int, int)>.Failed(default, PatternMismatch(pattern, fieldName));
        var match = matches.Current;
        return ParseAttempt<(int, int)>.Success((match.Index, match.Length));
    }

    private ParseAttempt<Regex> PreparePatternStatus(
        ReadOnlySpan<char> text,
        PreparedTextPattern pattern,
        string fieldName
    )
    {
        if (CheckTextPositionStatus(text, fieldName) is { } position)
            return ParseAttempt<Regex>.Failed(default, position);
        if (pattern.HasConflictingQuantifiers)
            return FailText<Regex>(
                ParseErrorCode.GeneralError,
                fieldName,
                "Pattern fields cannot be both greedy and lazy"
            );

        try
        {
            return ParseAttempt<Regex>.Success(pattern.Regex);
        }
        catch (ArgumentException exception)
        {
            return InvalidPattern(pattern, fieldName, "is invalid", exception);
        }
        catch (NotSupportedException exception)
        {
            return InvalidPattern(pattern, fieldName, "uses an unsupported construct", exception);
        }
    }

    private ParseAttempt<Regex> InvalidPattern(
        PreparedTextPattern pattern,
        string fieldName,
        string reason,
        Exception exception
    )
    {
        var subject = pattern.IsLookahead ? "Switch lookahead pattern" : "Pattern";
        return ParseAttempt<Regex>.Failed(
            default,
            ParseFailure.ForText(
                ParseErrorCode.GeneralError,
                SchemaName,
                fieldName,
                ParsePosition,
                $"{subject} '{pattern.Pattern}' {reason}: {exception.Message}",
                exception
            )
        );
    }

    private ParseFailure PatternMismatch(PreparedTextPattern pattern, string fieldName) =>
        ParseFailure.ForText(
            ParseErrorCode.PatternMismatch,
            SchemaName,
            fieldName,
            ParsePosition,
            $"Pattern '{pattern.Pattern}' did not match at parse position {ParsePosition}"
        );

    private static string ApplyPreparedModifiers(string value, TextPatternModifiers modifiers) =>
        ApplyModifiers(
            value,
            modifiers.HasFlag(TextPatternModifiers.TrimStart),
            modifiers.HasFlag(TextPatternModifiers.TrimEnd),
            modifiers.HasFlag(TextPatternModifiers.Lower),
            modifiers.HasFlag(TextPatternModifiers.Upper)
        );
}
