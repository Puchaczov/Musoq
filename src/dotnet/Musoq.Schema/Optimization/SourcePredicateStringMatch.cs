namespace Musoq.Schema.Optimization;

/// <summary>Represents a source-local constant string match with explicit comparison semantics.</summary>
public sealed record SourcePredicateStringMatch : SourcePredicateExpression
{
    /// <summary>Initializes a source-local string match.</summary>
    /// <param name="column">The source column read by the predicate.</param>
    /// <param name="kind">The classified match shape.</param>
    /// <param name="originalPattern">The original SQL <c>LIKE</c> pattern.</param>
    /// <param name="needle">The wildcard-free value used by the classified match.</param>
    /// <param name="comparison">The required comparison semantics.</param>
    /// <param name="isNegated">Whether the result is negated.</param>
    public SourcePredicateStringMatch(
        SourceColumnRef column,
        SourceStringMatchKind kind,
        string originalPattern,
        string needle,
        SourceStringComparison comparison = SourceStringComparison.LikeIgnoreCase,
        bool isNegated = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Name);
        ArgumentNullException.ThrowIfNull(originalPattern);
        ArgumentNullException.ThrowIfNull(needle);
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown source string-match kind.");
        if (comparison != SourceStringComparison.LikeIgnoreCase)
        {
            throw new ArgumentOutOfRangeException(
                nameof(comparison),
                comparison,
                "Source string matches currently require LIKE-equivalent case-insensitive comparison.");
        }

        Column = column;
        Kind = kind;
        OriginalPattern = originalPattern;
        Needle = needle;
        Comparison = comparison;
        IsNegated = isNegated;
    }

    /// <summary>Gets the source column read by the predicate.</summary>
    public SourceColumnRef Column { get; }

    /// <summary>Gets the classified match shape.</summary>
    public SourceStringMatchKind Kind { get; }

    /// <summary>Gets the original SQL <c>LIKE</c> pattern.</summary>
    public string OriginalPattern { get; }

    /// <summary>Gets the wildcard-free classified value.</summary>
    public string Needle { get; }

    /// <summary>Gets the required comparison semantics.</summary>
    public SourceStringComparison Comparison { get; }

    /// <summary>Gets whether the match result is negated.</summary>
    public bool IsNegated { get; }
}
