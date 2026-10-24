namespace Musoq.Schema.Optimization;

/// <summary>A source-local pattern predicate with explicit negation.</summary>
public abstract record SourcePredicatePatternMatch : SourcePredicateExpression
{
    /// <summary>Initializes a pattern predicate.</summary>
    protected SourcePredicatePatternMatch(SourceColumnRef column, bool isNegated)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Name);
        Column = column;
        IsNegated = isNegated;
    }

    /// <summary>Gets the source column.</summary>
    public SourceColumnRef Column { get; }

    /// <summary>Gets whether the match result is negated.</summary>
    public bool IsNegated { get; }
}
