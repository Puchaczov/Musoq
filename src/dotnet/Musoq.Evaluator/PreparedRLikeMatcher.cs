using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator;

/// <summary>Represents one lazily prepared RLIKE pattern.</summary>
public sealed class PreparedRLikeMatcher
{
    private const int AnchorLength = 2;
    private readonly Func<string, CultureInfo, TimeSpan, Regex> _regexFactory;
    private readonly RLikeLiteralMatchKind _literalKind;
    private object? _frontEntry;
    private int _regexResolutionCount;

    internal PreparedRLikeMatcher(
        string pattern,
        Func<string, CultureInfo, Regex> regexFactory)
        : this(pattern, RegexMatchBudget.Default, (value, culture, _) => regexFactory(value, culture))
    {
        ArgumentNullException.ThrowIfNull(regexFactory);
    }

    internal PreparedRLikeMatcher(
        string pattern,
        TimeSpan timeout,
        Func<string, CultureInfo, TimeSpan, Regex> regexFactory)
        : this(pattern, RegexMatchBudget.Create(timeout), regexFactory)
    {
    }

    internal PreparedRLikeMatcher(
        string pattern,
        RegexMatchBudget budget,
        Func<string, CultureInfo, TimeSpan, Regex> regexFactory)
    {
        ArgumentNullException.ThrowIfNull(budget);
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        _regexFactory = regexFactory ?? throw new ArgumentNullException(nameof(regexFactory));
        _literalKind = RLikeLiteralMatchKind.Regex;
        if (RLikeLiteralPatternClassifier.Classify(pattern).Classification is { } literal)
        {
            _literalKind = literal.Kind;
            _frontEntry = budget;
            return;
        }
        _frontEntry = (object?)Operators.GetOrCreateSimpleRLikeMatcher(pattern, budget.Timeout) ?? budget;
    }

    /// <summary>Gets the original regular-expression pattern.</summary>
    public string Pattern { get; }

    internal TimeSpan MatchTimeout => Volatile.Read(ref _frontEntry) switch
    {
        RegexMatchBudget budget => budget.Timeout,
        RLikeSimpleMatcher simpleMatcher => simpleMatcher.MatchTimeout,
        CultureEntry cached => cached.Regex.MatchTimeout,
        _ => throw new InvalidOperationException("The prepared RLIKE matcher has no match budget.")
    };

    internal Regex? ResolvedRegex => (Volatile.Read(ref _frontEntry) as CultureEntry)?.Regex;

    internal int RegexResolutionCount => Volatile.Read(ref _regexResolutionCount);

    internal bool IsUnanchoredLiteral => _literalKind == RLikeLiteralMatchKind.Contains;

    /// <summary>Matches a non-null input under the current culture.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMatch(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (_literalKind != RLikeLiteralMatchKind.Regex)
            return MatchLiteral(input);

        return MatchNonLiteral(input);
    }

    private bool MatchNonLiteral(string input)
    {
        var frontEntry = Volatile.Read(ref _frontEntry);
        if (frontEntry is RLikeSimpleMatcher simpleMatcher)
            return simpleMatcher.IsMatch(input);

        var culture = CultureInfo.CurrentCulture;
        if (frontEntry is CultureEntry cached && cached.Matches(culture))
            return RegexMatchExecutor.IsMatch(cached.Regex, input, "RLIKE");

        return RegexMatchExecutor.IsMatch(ResolveRegex(culture), input, "RLIKE");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchLiteral(string input)
    {
        return _literalKind switch
        {
            RLikeLiteralMatchKind.Exact => MatchExactLiteral(input),
            RLikeLiteralMatchKind.Prefix => input.AsSpan().StartsWith(Pattern.AsSpan(AnchorLength)),
            RLikeLiteralMatchKind.Suffix => input.AsSpan().EndsWith(Pattern.AsSpan(0, Pattern.Length - AnchorLength)),
            RLikeLiteralMatchKind.Contains => MatchContainedLiteral(input),
            _ => throw new ArgumentOutOfRangeException(nameof(_literalKind), _literalKind, "Unknown literal RLIKE strategy.")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchExactLiteral(string input)
    {
        if (input.Length != Pattern.Length - 2 * AnchorLength)
            return false;
        return input.AsSpan().SequenceEqual(Pattern.AsSpan(AnchorLength, input.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MatchContainedLiteral(string input)
    {
        if (input.Length == Pattern.Length)
            return string.Equals(input, Pattern, StringComparison.Ordinal);
        return input.Contains(Pattern, StringComparison.Ordinal);
    }

    private Regex ResolveRegex(CultureInfo culture)
    {
        Regex regex;
        try
        {
            regex = _regexFactory(Pattern, culture, MatchTimeout);
        }
        catch (RegexParseException exception)
        {
            throw new InvalidRuntimeRegexException(exception);
        }
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
