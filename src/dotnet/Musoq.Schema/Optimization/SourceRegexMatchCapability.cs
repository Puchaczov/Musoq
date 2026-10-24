namespace Musoq.Schema.Optimization;

/// <summary>Opt-in support for Core-equivalent constant RLIKE predicates on one column.</summary>
public sealed record SourceRegexMatchCapability
{
    /// <summary>Initializes a row-filtering capability. Providers may still reject individual patterns.</summary>
    public SourceRegexMatchCapability(
        SourceColumnRef column,
        SourceRegexSemantics semantics = SourceRegexSemantics.MusoqRLike,
        bool supportsNegation = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Name);
        if (semantics != SourceRegexSemantics.MusoqRLike)
            throw new ArgumentOutOfRangeException(nameof(semantics));
        Column = column;
        Semantics = semantics;
        SupportsNegation = supportsNegation;
    }

    /// <summary>Gets the source column.</summary>
    public SourceColumnRef Column { get; }

    /// <summary>Gets the supported semantics.</summary>
    public SourceRegexSemantics Semantics { get; }

    /// <summary>Gets whether negated matches are supported.</summary>
    public bool SupportsNegation { get; }

    /// <summary>Tests the column, semantics, negation, and row-filtering phase.</summary>
    public bool Supports(SourcePredicateRegexMatch predicate, SourcePredicateEvaluationPhase phase)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return phase == SourcePredicateEvaluationPhase.RowFiltering &&
               string.Equals(Column.Name, predicate.Column.Name, StringComparison.OrdinalIgnoreCase) &&
               Semantics == predicate.Semantics &&
               (!predicate.IsNegated || SupportsNegation);
    }
}
