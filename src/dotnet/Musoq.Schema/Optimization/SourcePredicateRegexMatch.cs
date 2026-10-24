namespace Musoq.Schema.Optimization;

/// <summary>A constant RLIKE pattern applied to one source column.</summary>
public sealed record SourcePredicateRegexMatch : SourcePredicatePatternMatch
{
    /// <summary>Initializes a regex predicate. Pattern syntax is validated by semantic binding.</summary>
    public SourcePredicateRegexMatch(
        SourceColumnRef column,
        string pattern,
        TimeSpan matchTimeout,
        SourceRegexSemantics semantics = SourceRegexSemantics.MusoqRLike,
        bool isNegated = false)
        : base(column, isNegated)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (semantics != SourceRegexSemantics.MusoqRLike)
            throw new ArgumentOutOfRangeException(nameof(semantics));
        if (matchTimeout < TimeSpan.FromMilliseconds(1) || matchTimeout.TotalMilliseconds > int.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(matchTimeout), "A finite positive regex timeout is required.");
        Pattern = pattern;
        MatchTimeout = matchTimeout;
        Semantics = semantics;
    }

    /// <summary>Gets the original .NET regex pattern, including inline options.</summary>
    public string Pattern { get; }

    /// <summary>Gets the finite per-match timeout required by Core.</summary>
    public TimeSpan MatchTimeout { get; }

    /// <summary>Gets the required regex semantics.</summary>
    public SourceRegexSemantics Semantics { get; }
}
