using System.Globalization;
using System.Text;

namespace Musoq.Evaluator;

/// <summary>Linear, ordinal matching for a deliberately restricted regular-expression grammar.</summary>
internal sealed class RLikeSimpleMatcher(
    string[] prefixes, string suffix, RLikeRepeatSpecification repetition, TimeSpan timeout)
{
    internal TimeSpan MatchTimeout { get; } = timeout;

    public bool IsMatch(string input)
    {
        var value = input.AsSpan();
        if (repetition.Kind == RLikeSimpleCharacterKind.LiteralSuffix)
        {
            if (value.EndsWith(suffix, StringComparison.Ordinal))
                return true;
            return value.Length > 0 && value[^1] == '\n' &&
                   value[..^1].EndsWith(suffix, StringComparison.Ordinal);
        }

        foreach (var prefix in prefixes)
        {
            if (!value.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            var remainder = value[prefix.Length..];
            if (remainder.Length < repetition.Bounds.Minimum || remainder.Length > repetition.Bounds.Maximum)
                continue;
            if (MatchesCharacters(remainder))
                return true;
        }
        return false;
    }

    private bool MatchesCharacters(ReadOnlySpan<char> value) => repetition.Kind switch
    {
        RLikeSimpleCharacterKind.AsciiDigits => value.IndexOfAnyExceptInRange('0', '9') < 0,
        RLikeSimpleCharacterKind.ExceptNewline => !value.Contains('\n'),
        _ => throw new InvalidOperationException("A literal suffix has no repeated character class.")
    };

    public static RLikeSimpleMatcher? TryCreate(string pattern) =>
        TryCreate(pattern, Runtime.RuntimeCacheOptions.DefaultRegexTimeout);

    public static RLikeSimpleMatcher? TryCreate(string pattern, TimeSpan timeout)
    {
        var suffixMatcher = TryCreateUnanchoredSuffix(pattern, timeout);
        if (suffixMatcher is not null)
            return suffixMatcher;

        if (!pattern.StartsWith(@"\A", StringComparison.Ordinal) ||
            !pattern.EndsWith(@"\z", StringComparison.Ordinal))
            return null;
        var body = pattern.AsSpan(2, pattern.Length - 4);
        var repeatStart = body.LastIndexOf("[0-9]", StringComparison.Ordinal);
        var digits = repeatStart >= 0;
        if (!digits)
            repeatStart = body.LastIndexOf('.');
        if (repeatStart < 0)
            return null;
        var quantifier = body[(repeatStart + (digits ? 5 : 1))..];
        var repeatBounds = ParseBoundsOrReject(quantifier);
        if (repeatBounds is null)
            return null;
        var prefix = body[..repeatStart];
        var alternatives = ParsePrefixesOrReject(prefix);
        if (alternatives is null)
            return null;
        var characterKind = digits ? RLikeSimpleCharacterKind.AsciiDigits : RLikeSimpleCharacterKind.ExceptNewline;
        var specification = new RLikeRepeatSpecification(characterKind, repeatBounds.Value);
        return new RLikeSimpleMatcher(alternatives, string.Empty, specification, timeout);
    }

    /// <summary>An unanchored .* can start at the suffix itself, including after a newline.</summary>
    private static RLikeSimpleMatcher? TryCreateUnanchoredSuffix(string pattern, TimeSpan timeout)
    {
        if (!pattern.StartsWith(".*", StringComparison.Ordinal) || !pattern.EndsWith('$'))
            return null;
        var suffix = DecodeLiteralOrReject(pattern.AsSpan(2, pattern.Length - 3));
        if (suffix is null)
            return null;
        return new RLikeSimpleMatcher([], suffix, new(RLikeSimpleCharacterKind.LiteralSuffix, default), timeout);
    }

    private static string[]? ParsePrefixesOrReject(ReadOnlySpan<char> prefix)
    {
        if (!prefix.StartsWith("(?:", StringComparison.Ordinal))
        {
            var literal = DecodeLiteralOrReject(prefix);
            if (literal is null)
                return null;
            return [literal];
        }

        var close = prefix.IndexOf(')');
        if (close < 0)
            return null;
        var tail = DecodeLiteralOrReject(prefix[(close + 1)..]);
        if (tail is null)
            return null;
        var alternatives = prefix[3..close].ToString().Split('|');
        for (var index = 0; index < alternatives.Length; index++)
        {
            var literal = DecodeLiteralOrReject(alternatives[index]);
            if (literal is null)
                return null;
            alternatives[index] = literal + tail;
        }
        return alternatives;
    }

    private static RLikeRepeatBounds? ParseBoundsOrReject(ReadOnlySpan<char> quantifier)
    {
        if (quantifier.SequenceEqual("*"))
            return new RLikeRepeatBounds(0, int.MaxValue);
        if (quantifier.SequenceEqual("+"))
            return new RLikeRepeatBounds(1, int.MaxValue);
        if (quantifier.Length < 3 || quantifier[0] != '{' || quantifier[^1] != '}')
            return null;
        var count = quantifier[1..^1];
        foreach (var character in count)
            if (character is < '0' or > '9')
                return null;
        if (!int.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out var exactCount))
            return null;
        return new RLikeRepeatBounds(exactCount, exactCount);
    }

    private static string? DecodeLiteralOrReject(ReadOnlySpan<char> text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\\')
            {
                if (++index == text.Length || !IsMetacharacter(text[index]) && text[index] != '\\')
                    return null;
                character = text[index];
            }
            else if (IsMetacharacter(character))
                return null;
            builder.Append(character);
        }
        return builder.ToString();
    }

    private static bool IsMetacharacter(char character) => character is
        '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}';
}

internal readonly record struct RLikeRepeatBounds(int Minimum, int Maximum);

internal readonly record struct RLikeRepeatSpecification(RLikeSimpleCharacterKind Kind, RLikeRepeatBounds Bounds);

internal enum RLikeSimpleCharacterKind
{
    LiteralSuffix,
    AsciiDigits,
    ExceptNewline
}
