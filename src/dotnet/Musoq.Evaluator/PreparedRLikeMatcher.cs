using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Musoq.Evaluator;

/// <summary>Represents one lazily prepared RLIKE pattern.</summary>
public sealed class PreparedRLikeMatcher
{
    private readonly Func<string, CultureInfo, Regex> _regexFactory;
    private readonly RLikeLiteralClassification? _literalClassification;
    private CultureEntry? _frontEntry;
    private int _regexResolutionCount;

    internal PreparedRLikeMatcher(
        string pattern,
        Func<string, CultureInfo, Regex> regexFactory)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _regexFactory = regexFactory ?? throw new ArgumentNullException(nameof(regexFactory));
        _literalClassification = RLikeLiteralPatternClassifier.Classify(pattern).Classification;
    }

    /// <summary>Gets the original regular-expression pattern.</summary>
    public string Pattern { get; }

    internal int RegexResolutionCount => Volatile.Read(ref _regexResolutionCount);

    /// <summary>Matches a non-null input under the current culture.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMatch(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (_literalClassification is { } literal)
            return MatchLiteral(input, literal);

        var culture = CultureInfo.CurrentCulture;
        var frontEntry = Volatile.Read(ref _frontEntry);
        if (frontEntry is not null && frontEntry.Matches(culture))
            return frontEntry.Regex.IsMatch(input);

        return ResolveRegex(culture).IsMatch(input);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchLiteral(string input, RLikeLiteralClassification literal)
    {
        var value = input.AsSpan();
        var pattern = Pattern.AsSpan(literal.LiteralStart, literal.LiteralLength);
        return literal.Kind switch
        {
            RLikeLiteralMatchKind.Exact => value.Equals(pattern, StringComparison.Ordinal),
            RLikeLiteralMatchKind.Prefix => value.StartsWith(pattern, StringComparison.Ordinal),
            RLikeLiteralMatchKind.Suffix => value.EndsWith(pattern, StringComparison.Ordinal),
            RLikeLiteralMatchKind.Contains => value.Contains(pattern, StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(literal), literal.Kind, "Unknown literal RLIKE strategy.")
        };
    }

    private Regex ResolveRegex(CultureInfo culture)
    {
        var regex = _regexFactory(Pattern, culture);
        Interlocked.Increment(ref _regexResolutionCount);
        Volatile.Write(ref _frontEntry, new CultureEntry(culture, regex));
        return regex;
    }

    private sealed record CultureEntry(CultureInfo Culture, Regex Regex)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(CultureInfo culture) =>
            ReferenceEquals(Culture, culture) ||
            string.Equals(Culture.Name, culture.Name, StringComparison.Ordinal);
    }
}
