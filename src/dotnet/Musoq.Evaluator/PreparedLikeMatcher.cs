using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Musoq.Evaluator;

/// <summary>Represents one immutable LIKE pattern prepared under the current culture.</summary>
public sealed class PreparedLikeMatcher
{
    private readonly LikePatternClassification? _classification;
    private readonly Func<string, bool>? _legacyMatcher;
    private readonly Lazy<Func<string, bool>>? _asciiLegacyMatcher;
    private readonly TextInfo _textInfo;
    private readonly bool _ordinalIgnoreCaseCompatible;

    internal PreparedLikeMatcher(
        string originalPattern,
        LikePatternClassificationResult classification,
        Func<string, bool>? legacyMatcher,
        CultureInfo preparationCulture)
    {
        ArgumentNullException.ThrowIfNull(originalPattern);

        OriginalPattern = originalPattern;
        _classification = classification.Classification;
        _legacyMatcher = legacyMatcher;
        if (_classification is { } classifiedMatch)
        {
            LiteralStart = classifiedMatch.LiteralStart;
            LiteralLength = classifiedMatch.LiteralLength;
        }
        else
        {
            LiteralStart = 0;
            LiteralLength = originalPattern.Length;
        }

        var literal = originalPattern.AsSpan(LiteralStart, LiteralLength);
        var textInfo = preparationCulture.TextInfo;
        _textInfo = textInfo;
        _ordinalIgnoreCaseCompatible = literal.IndexOfAny('I', 'i') < 0 ||
                                       textInfo.ToLower('I') == 'i' && textInfo.ToUpper('i') == 'I';
        if (_classification is { Domain: LikePatternCharacterDomain.Ascii })
        {
            _asciiLegacyMatcher = new Lazy<Func<string, bool>>(
                () => Operators.CreateLegacyLikeMatcherUnderCulture(OriginalPattern, preparationCulture),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }

    /// <summary>Gets the original SQL LIKE pattern.</summary>
    public string OriginalPattern { get; }

    /// <summary>Gets the zero-based start of the classified literal in <see cref="OriginalPattern"/>.</summary>
    public int LiteralStart { get; }

    /// <summary>Gets the number of UTF-16 code units in the classified literal.</summary>
    public int LiteralLength { get; }

    internal bool IsSingleAsciiPrefix =>
        _classification is
        {
            Domain: LikePatternCharacterDomain.Ascii,
            Kind: LikePatternMatchKind.Prefix
        } &&
        LiteralLength == 1 &&
        _ordinalIgnoreCaseCompatible;

    internal char SingleAsciiPrefix => OriginalPattern[LiteralStart];

    /// <summary>Matches a non-null input using the semantics captured when this matcher was prepared.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMatch(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_classification is not { } match)
            return RequiredLegacyMatcher(content);

        return match.Domain == LikePatternCharacterDomain.Ascii
            ? MatchAscii(content, match.Kind)
            : MatchUnicode(content, match.Kind);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchAscii(string content, LikePatternMatchKind kind)
    {
        if (content.Length < LiteralLength || kind == LikePatternMatchKind.Exact && content.Length != LiteralLength)
            return false;
        if (LiteralLength == 0 && kind == LikePatternMatchKind.Contains)
            return true;

        if (LiteralLength == 1 && kind != LikePatternMatchKind.Contains)
        {
            var comparisonIndex = kind == LikePatternMatchKind.Suffix ? content.Length - 1 : 0;
            var actual = content[comparisonIndex];
            var expected = OriginalPattern[LiteralStart];
            if (_ordinalIgnoreCaseCompatible && actual <= 0x7f)
                return AsciiEqualsIgnoreCase(actual, expected);

            return Operators.MayMatchAsciiLiteralUnderCulture(
                       content.AsSpan(),
                       OriginalPattern.AsSpan(LiteralStart, LiteralLength),
                       kind,
                       _textInfo) &&
                   RequiredAsciiLegacyMatcher(content);
        }

        var comparisonStart = kind == LikePatternMatchKind.Suffix ? content.Length - LiteralLength : 0;
        var comparisonLength = kind == LikePatternMatchKind.Contains ? content.Length : LiteralLength;
        if (!_ordinalIgnoreCaseCompatible ||
            !Operators.IsAsciiLikeSpan(content, comparisonStart, comparisonLength))
        {
            return Operators.MayMatchAsciiLiteralUnderCulture(
                       content.AsSpan(),
                       OriginalPattern.AsSpan(LiteralStart, LiteralLength),
                       kind,
                       _textInfo) &&
                   RequiredAsciiLegacyMatcher(content);
        }

        var literal = OriginalPattern.AsSpan(LiteralStart, LiteralLength);
        return kind switch
        {
            LikePatternMatchKind.Exact => LiteralLength == 1
                ? AsciiEqualsIgnoreCase(content[0], OriginalPattern[LiteralStart])
                : content.AsSpan().Equals(literal, StringComparison.OrdinalIgnoreCase),
            LikePatternMatchKind.Prefix => LiteralLength == 1
                ? AsciiEqualsIgnoreCase(content[0], OriginalPattern[LiteralStart])
                : content.AsSpan().StartsWith(literal, StringComparison.OrdinalIgnoreCase),
            LikePatternMatchKind.Suffix => LiteralLength == 1
                ? AsciiEqualsIgnoreCase(content[^1], OriginalPattern[LiteralStart])
                : content.AsSpan().EndsWith(literal, StringComparison.OrdinalIgnoreCase),
            LikePatternMatchKind.Contains => content.AsSpan().Contains(literal, StringComparison.OrdinalIgnoreCase),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown LIKE match kind.")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AsciiEqualsIgnoreCase(char value, char expected)
    {
        if (value == expected)
            return true;

        var foldedValue = (uint)(value | 0x20);
        return foldedValue == (uint)(expected | 0x20) && foldedValue - 'a' <= 'z' - 'a';
    }

    private bool MatchUnicode(string content, LikePatternMatchKind kind)
    {
        var literal = OriginalPattern.AsSpan(LiteralStart, LiteralLength);
        var directMatch = kind switch
        {
            LikePatternMatchKind.Exact => content.AsSpan().Equals(literal, StringComparison.Ordinal),
            LikePatternMatchKind.Prefix => content.AsSpan().StartsWith(literal, StringComparison.Ordinal),
            LikePatternMatchKind.Suffix => content.AsSpan().EndsWith(literal, StringComparison.Ordinal),
            LikePatternMatchKind.Contains => content.AsSpan().Contains(literal, StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown LIKE match kind.")
        };

        return directMatch || RequiredLegacyMatcher(content);
    }

    private bool RequiredLegacyMatcher(string content)
    {
        if (_legacyMatcher is null)
            throw new InvalidOperationException("This prepared LIKE matcher does not have a legacy fallback.");

        return _legacyMatcher(content);
    }

    private bool RequiredAsciiLegacyMatcher(string content)
    {
        var matcher = _asciiLegacyMatcher
            ?? throw new InvalidOperationException("This prepared ASCII LIKE matcher has no legacy fallback.");
        return matcher.Value(content);
    }
}
