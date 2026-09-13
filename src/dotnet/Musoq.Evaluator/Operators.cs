using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator;

public partial class Operators
{
    private const int ScalarAsciiProbeLimit = 8;
    private static readonly BoundedRuntimeCache<LikeMatcherCacheKey, PreparedLikeMatcher> LikeMatcherCache =
        new(RuntimeCacheOptions.PatternCacheSize);
    private static readonly BoundedRuntimeCache<LikeMatcherCacheKey, Func<string, bool>> LegacyLikeMatcherCache =
        new(RuntimeCacheOptions.PatternCacheSize);
    private static readonly BoundedRuntimeCache<string, Regex> RLikePatternCache =
        new(RuntimeCacheOptions.PatternCacheSize, StringComparer.Ordinal);
    private static LegacyLikeMatcherFrontEntry? _legacyLikeMatcherFrontEntry;

    /// <summary>
    ///     Applies a typed comparison only when both operands are non-null, preserving SQL UNKNOWN otherwise.
    /// </summary>
    public static bool? SqlCompare<TLeft, TRight>(
        TLeft left,
        TRight right,
        Func<TLeft, TRight, bool?> comparison)
    {
        if (left is null || right is null)
            return null;

        return comparison(left, right);
    }

    public bool Like(string? content, string? searchFor)
    {
        return LikeDynamic(content, searchFor);
    }

    /// <summary>Matches a value against a runtime LIKE pattern using a bounded culture-aware matcher cache.</summary>
    public static bool LikeDynamic(string? content, string? searchFor)
    {
        if (content is null || searchFor is null)
            return false;

        var key = CreateLikeMatcherCacheKey(searchFor);
        var matcher = LikeMatcherCache.GetOrAdd(key, static cacheKey => CreateLikeMatcher(cacheKey.Pattern));
        return matcher.IsMatch(content);
    }

    /// <summary>Prepares a LIKE pattern under the current culture, or returns null for a null SQL pattern.</summary>
    public static PreparedLikeMatcher? PrepareLike(string? pattern)
    {
        if (pattern is null)
            return null;

        var key = CreateLikeMatcherCacheKey(pattern);
        if (LikeMatcherCache.TryGetValue(key, out var cachedMatcher))
            return cachedMatcher;

        var classification = LikePatternClassifier.Classify(pattern);
        if (classification.Classification is { Domain: LikePatternCharacterDomain.Ascii })
            return CreateLikeMatcher(pattern, classification);

        return LikeMatcherCache.GetOrAdd(key, static cacheKey => CreateLikeMatcher(cacheKey.Pattern));
    }

    /// <summary>Matches a value using an already prepared LIKE matcher.</summary>
    public static bool LikePrepared(string? content, PreparedLikeMatcher? matcher)
    {
        return content is not null && matcher is not null && matcher.IsMatch(content);
    }

    /// <summary>Matches a value against a runtime LIKE pattern using an execution-local two-entry cache.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LikeDynamic(string? content, string? searchFor, LikeMatcherCacheSlot cacheSlot)
    {
        if (content is null || searchFor is null)
            return false;

        ArgumentNullException.ThrowIfNull(cacheSlot);
        return cacheSlot.IsMatch(content, searchFor);
    }

    /// <summary>Matches one non-null value against an exact ASCII LIKE literal.</summary>
    public static bool LikeExact(string? content, string? literal)
    {
        if (content is null || literal is null || content.Length != literal.Length)
            return false;

        var ordinalCompatible = IsOrdinalIgnoreCaseLikeCompatible(literal.AsSpan());
        if (ordinalCompatible && string.Equals(content, literal, StringComparison.OrdinalIgnoreCase))
            return true;

        return (!ordinalCompatible || !IsAscii(content.AsSpan())) &&
               LikeLegacyRegexExact(content, literal, literal);
    }

    /// <summary>Matches one non-null value against an ASCII LIKE prefix.</summary>
    public static bool LikePrefix(string? content, string? literal)
    {
        if (content is null || literal is null || content.Length < literal.Length)
            return false;

        var ordinalCompatible = IsOrdinalIgnoreCaseLikeCompatible(literal.AsSpan());
        if (ordinalCompatible && content.StartsWith(literal, StringComparison.OrdinalIgnoreCase))
            return true;

        return (!ordinalCompatible || !IsAscii(content.AsSpan(0, literal.Length))) &&
               LikeLegacyRegexPrefix(content, string.Concat(literal, "%"), literal);
    }

    /// <summary>Matches one non-null value against an ASCII LIKE suffix.</summary>
    public static bool LikeSuffix(string? content, string? literal)
    {
        if (content is null || literal is null || content.Length < literal.Length)
            return false;

        var ordinalCompatible = IsOrdinalIgnoreCaseLikeCompatible(literal.AsSpan());
        if (ordinalCompatible && content.EndsWith(literal, StringComparison.OrdinalIgnoreCase))
            return true;

        return (!ordinalCompatible || !IsAscii(content.AsSpan(content.Length - literal.Length, literal.Length))) &&
               LikeLegacyRegexSuffix(content, string.Concat("%", literal), literal);
    }

    /// <summary>Matches one non-null value against an ASCII LIKE contains literal.</summary>
    public static bool LikeContains(string? content, string? literal)
    {
        if (content is null || literal is null || content.Length < literal.Length)
            return false;
        if (literal.Length == 0)
            return true;

        var ordinalCompatible = IsOrdinalIgnoreCaseLikeCompatible(literal.AsSpan());
        if (ordinalCompatible && content.Contains(literal, StringComparison.OrdinalIgnoreCase))
            return true;

        return (!ordinalCompatible || !IsAscii(content.AsSpan())) &&
               LikeLegacyRegexContains(content, string.Concat("%", literal, "%"), literal);
    }

    /// <summary>Matches a value with the historical regular-expression LIKE semantics.</summary>
    public static bool LikeLegacyRegex(string? content, string? pattern)
    {
        if (content is null || pattern is null)
            return false;

        var culture = CultureInfo.CurrentCulture;
        var frontEntry = Volatile.Read(ref _legacyLikeMatcherFrontEntry);
        if (frontEntry is not null && frontEntry.Matches(pattern, culture))
            return frontEntry.Matcher(content);

        var cultureName = culture.Name;
        var key = new LikeMatcherCacheKey(pattern, cultureName);
        var matcher = LegacyLikeMatcherCache
            .GetOrAdd(key, static cacheKey => CreateLegacyLikeMatcher(cacheKey.Pattern));
        Volatile.Write(
            ref _legacyLikeMatcherFrontEntry,
            new LegacyLikeMatcherFrontEntry(pattern, culture, matcher));
        return matcher(content);
    }

    /// <summary>Runs the legacy exact matcher after a conservative culture-aware rejection check.</summary>
    public static bool LikeLegacyRegexExact(string? content, string? pattern, string? literal) =>
        LikeLegacyRegexAfterAsciiMiss(content, pattern, literal, LikePatternMatchKind.Exact);

    /// <summary>Runs the legacy prefix matcher after a conservative culture-aware rejection check.</summary>
    public static bool LikeLegacyRegexPrefix(string? content, string? pattern, string? literal) =>
        LikeLegacyRegexAfterAsciiMiss(content, pattern, literal, LikePatternMatchKind.Prefix);

    /// <summary>Runs the legacy one-code-unit prefix matcher after a conservative casing rejection check.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LikeLegacyRegexSingleAsciiPrefix(
        string? content,
        string? pattern,
        string? literal)
    {
        if (content is null || pattern is null || literal is null || content.Length == 0 || literal.Length != 1)
            return false;

        var expected = literal[0];
        var mayMatch = expected is 'I' or 'i'
            ? MayMatchAsciiCharacterUnderRegexCasing(
                content[0],
                expected,
                CultureInfo.CurrentCulture.TextInfo)
            : MayMatchInvariantAsciiCharacter(content[0], expected);

        return mayMatch &&
               LikeLegacyRegex(content, pattern);
    }

    /// <summary>Runs the legacy suffix matcher after a conservative culture-aware rejection check.</summary>
    public static bool LikeLegacyRegexSuffix(string? content, string? pattern, string? literal) =>
        LikeLegacyRegexAfterAsciiMiss(content, pattern, literal, LikePatternMatchKind.Suffix);

    /// <summary>Runs the legacy contains matcher after a conservative culture-aware rejection check.</summary>
    public static bool LikeLegacyRegexContains(string? content, string? pattern, string? literal) =>
        LikeLegacyRegexAfterAsciiMiss(content, pattern, literal, LikePatternMatchKind.Contains);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool MayMatchAsciiLiteralUnderCulture(
        ReadOnlySpan<char> content,
        ReadOnlySpan<char> literal,
        LikePatternMatchKind kind,
        TextInfo textInfo)
    {
        ArgumentNullException.ThrowIfNull(textInfo);

        return kind switch
        {
            LikePatternMatchKind.Exact => content.Length == literal.Length &&
                                          MayEqualUnderRegexCasing(content, literal, textInfo),
            LikePatternMatchKind.Prefix => content.Length >= literal.Length &&
                                           MayEqualUnderRegexCasing(content[..literal.Length], literal, textInfo),
            LikePatternMatchKind.Suffix => content.Length >= literal.Length &&
                                           MayEqualUnderRegexCasing(content[^literal.Length..], literal, textInfo),
            LikePatternMatchKind.Contains => MayContainUnderRegexCasing(content, literal, textInfo),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown LIKE match kind.")
        };
    }

    /// <summary>Returns whether the selected string span contains ASCII code units only.</summary>
    public static bool IsAsciiLikeSpan(string content, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(content);
        return IsAscii(content.AsSpan(start, length));
    }

    /// <summary>Returns whether ordinal-ignore-case ASCII results equal LIKE casing in the current culture.</summary>
    public static bool IsOrdinalIgnoreCaseLikeCompatible()
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToLower('I') == 'i' && textInfo.ToUpper('i') == 'I';
    }

    internal static bool IsOrdinalIgnoreCaseLikeCompatible(ReadOnlySpan<char> literal)
    {
        return literal.IndexOfAny('I', 'i') < 0 || IsOrdinalIgnoreCaseLikeCompatible();
    }

    public bool RLike(string? content, string? pattern)
    {
        if (content is null || pattern is null)
            return false;

        var regex = RLikePatternCache.GetOrAdd(pattern, static p =>
            new Regex(p, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout));

        return regex.IsMatch(content);
    }

    public bool Contains<T>(T? value, T?[]? values)
    {
        if (values is null)
            return false;

        return Array.IndexOf(values, value) >= 0;
    }

    private static PreparedLikeMatcher CreateLikeMatcher(string pattern)
    {
        var result = LikePatternClassifier.Classify(pattern);
        return CreateLikeMatcher(pattern, result);
    }

    private static PreparedLikeMatcher CreateLikeMatcher(
        string pattern,
        LikePatternClassificationResult result)
    {
        var requiresPreparedLegacyMatcher = result.Classification is not { } match ||
                                            match.Domain == LikePatternCharacterDomain.Unicode;
        var legacyMatcher = requiresPreparedLegacyMatcher ? CreateLegacyLikeMatcher(pattern) : null;
        return new PreparedLikeMatcher(pattern, result, legacyMatcher, CultureInfo.CurrentCulture);
    }

    private static LikeMatcherCacheKey CreateLikeMatcherCacheKey(string pattern) =>
        new(pattern, CultureInfo.CurrentCulture.Name);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LikeLegacyRegexAfterAsciiMiss(
        string? content,
        string? pattern,
        string? literal,
        LikePatternMatchKind kind)
    {
        if (content is null || pattern is null || literal is null)
            return false;

        return MayMatchAsciiLiteralUnderCulture(
                   content.AsSpan(),
                   literal.AsSpan(),
                   kind,
                   CultureInfo.CurrentCulture.TextInfo) &&
               LikeLegacyRegex(content, pattern);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MayContainUnderRegexCasing(
        ReadOnlySpan<char> content,
        ReadOnlySpan<char> literal,
        TextInfo textInfo)
    {
        if (literal.Length == 0)
            return true;

        for (var start = 0; start <= content.Length - literal.Length; start++)
        {
            if (MayEqualUnderRegexCasing(content.Slice(start, literal.Length), literal, textInfo))
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MayEqualUnderRegexCasing(
        ReadOnlySpan<char> content,
        ReadOnlySpan<char> literal,
        TextInfo textInfo)
    {
        for (var index = 0; index < literal.Length; index++)
        {
            var actual = content[index];
            var expected = literal[index];
            if (actual == expected)
                continue;
            if (MayMatchAsciiCharacterUnderRegexCasing(actual, expected, textInfo))
                continue;

            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MayMatchAsciiCharacterUnderRegexCasing(
        char actual,
        char expected,
        TextInfo textInfo)
    {
        if (actual == expected)
            return true;
        if (expected is 'I' or 'i')
        {
            return textInfo.ToLower(actual) == textInfo.ToLower(expected) ||
                   textInfo.ToUpper(actual) == textInfo.ToUpper(expected);
        }

        return MayMatchInvariantAsciiCharacter(actual, expected);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MayMatchInvariantAsciiCharacter(char actual, char expected)
    {
        if (actual == expected)
            return true;

        var expectedLower = expected is >= 'A' and <= 'Z' ? (char)(expected | 0x20) : expected;
        if (actual <= 0x7f)
        {
            var actualLower = actual is >= 'A' and <= 'Z' ? (char)(actual | 0x20) : actual;
            return actualLower == expectedLower;
        }

        return expectedLower switch
        {
            'k' => actual == '\u212A',
            's' => actual == '\u017F',
            _ => false
        };
    }

    private static Func<string, bool> CreateLegacyLikeMatcher(string pattern)
    {
        var regexPattern = CreateOptimizedLegacyLikeRegexPattern(pattern);
        var options = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;
        Regex regex;
        if (CanUseTimeoutBoundedBacktrackingMatcher(pattern))
        {
            regex = new Regex(regexPattern, options, RuntimeCacheOptions.DefaultRegexTimeout);
            return regex.IsMatch;
        }

        try
        {
            regex = new Regex(
                regexPattern,
                options | RegexOptions.NonBacktracking,
                RuntimeCacheOptions.DefaultRegexTimeout);
        }
        catch (NotSupportedException)
        {
            // The non-backtracking engine rejects automata above its fixed node limit.
            // LIKE patterns are escaped before wildcard expansion, so the timeout-bounded
            // backtracking engine is the compatible fallback for those large patterns.
            regex = new Regex(regexPattern, options, RuntimeCacheOptions.DefaultRegexTimeout);
        }

        return regex.IsMatch;
    }

    internal static bool CanUseTimeoutBoundedBacktrackingMatcher(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var classification = LikePatternClassifier.Classify(pattern).Classification;
        if (classification is not { } match || match.LiteralLength == 0)
            return false;

        var boundaryWildcardCount = match.Kind switch
        {
            LikePatternMatchKind.Exact => 0,
            LikePatternMatchKind.Prefix or LikePatternMatchKind.Suffix => 1,
            LikePatternMatchKind.Contains => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), match.Kind, "Unknown LIKE match kind.")
        };
        return pattern.Length == match.LiteralLength + boundaryWildcardCount;
    }

    internal static string CreateOptimizedLegacyLikeRegexPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var classification = LikePatternClassifier.Classify(pattern).Classification;
        if (classification is not { } match || !CanUseTimeoutBoundedBacktrackingMatcher(pattern))
            return CreateLegacyLikeRegexPattern(pattern);

        var builder = new StringBuilder(checked(match.LiteralLength * 2 + 4));
        if (match.Kind is LikePatternMatchKind.Exact or LikePatternMatchKind.Prefix)
            builder.Append(@"\A");

        AppendEscapedRegexLiteral(
            builder,
            pattern.AsSpan(match.LiteralStart, match.LiteralLength));

        if (match.Kind is LikePatternMatchKind.Exact or LikePatternMatchKind.Suffix)
            builder.Append(@"\z");

        return builder.ToString();
    }

    internal static string CreateLegacyLikeRegexPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var builder = new StringBuilder(checked(pattern.Length * 2 + 4));
        builder.Append(@"\A");
        foreach (var character in pattern)
        {
            switch (character)
            {
                case '%':
                    builder.Append(".*");
                    break;
                case '_':
                    builder.Append('.');
                    break;
                case '.':
                case '$':
                case '^':
                case '{':
                case '[':
                case '(':
                case '|':
                case ')':
                case '*':
                case '+':
                case '?':
                case '\\':
                    builder.Append('\\').Append(character);
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.Append(@"\z").ToString();
    }

    private static void AppendEscapedRegexLiteral(StringBuilder builder, ReadOnlySpan<char> literal)
    {
        foreach (var character in literal)
        {
            switch (character)
            {
                case '.':
                case '$':
                case '^':
                case '{':
                case '[':
                case '(':
                case '|':
                case ')':
                case '*':
                case '+':
                case '?':
                case '\\':
                    builder.Append('\\').Append(character);
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }
    }

    internal static Func<string, bool> CreateLegacyLikeMatcherUnderCulture(
        string pattern,
        CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(culture);

        var currentCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            return CreateLegacyLikeMatcher(pattern);
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAscii(ReadOnlySpan<char> value)
    {
        if (value.Length <= ScalarAsciiProbeLimit)
            return IsAsciiUpToEightCodeUnits(value);

        return Ascii.IsValid(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAsciiUpToEightCodeUnits(ReadOnlySpan<char> value)
    {
        return value.Length switch
        {
            0 => true,
            1 => value[0] <= 127,
            2 => (value[0] | value[1]) <= 127,
            3 => (value[0] | value[1] | value[2]) <= 127,
            4 => (value[0] | value[1] | value[2] | value[3]) <= 127,
            5 => (value[0] | value[1] | value[2] | value[3] | value[4]) <= 127,
            6 => (value[0] | value[1] | value[2] | value[3] | value[4] | value[5]) <= 127,
            7 => (value[0] | value[1] | value[2] | value[3] | value[4] | value[5] | value[6]) <= 127,
            8 => (value[0] | value[1] | value[2] | value[3] | value[4] | value[5] | value[6] | value[7]) <= 127,
            _ => false
        };
    }

    private readonly record struct LikeMatcherCacheKey(string Pattern, string CultureName);

    private sealed record LegacyLikeMatcherFrontEntry(
        string Pattern,
        CultureInfo Culture,
        Func<string, bool> Matcher)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(string pattern, CultureInfo culture) =>
            (ReferenceEquals(Pattern, pattern) || string.Equals(Pattern, pattern, StringComparison.Ordinal)) &&
            (ReferenceEquals(Culture, culture) ||
             string.Equals(Culture.Name, culture.Name, StringComparison.Ordinal));
    }
}
