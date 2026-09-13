namespace Musoq.Schema.Optimization;

/// <summary>Describes typed string-match operations advertised for one source column.</summary>
public sealed record SourceStringMatchCapability
{
    /// <summary>Initializes a string-match capability declaration.</summary>
    /// <param name="column">The metadata-backed source column.</param>
    /// <param name="operations">The supported match shapes.</param>
    /// <param name="comparison">The supported comparison semantics.</param>
    /// <param name="supportsNegation">Whether negated matches are supported.</param>
    /// <param name="phases">The phases at which the source can apply matches.</param>
    public SourceStringMatchCapability(
        SourceColumnRef column,
        SourceStringMatchOperations operations,
        SourceStringComparison comparison = SourceStringComparison.LikeIgnoreCase,
        bool supportsNegation = false,
        SourcePredicateEvaluationPhases phases = SourcePredicateEvaluationPhases.RowFiltering)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrWhiteSpace(column.Name);
        if (operations == SourceStringMatchOperations.None ||
            (operations & ~SourceStringMatchOperations.All) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(operations), operations, "Unknown source string-match operation.");
        }

        if (comparison != SourceStringComparison.LikeIgnoreCase)
        {
            throw new ArgumentOutOfRangeException(
                nameof(comparison),
                comparison,
                "Source string matches currently require LIKE-equivalent case-insensitive comparison.");
        }

        if (phases == SourcePredicateEvaluationPhases.None ||
            (phases & ~SourcePredicateEvaluationPhases.All) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(phases), phases, "Unknown source predicate evaluation phase.");
        }

        Column = column;
        Operations = operations;
        Comparison = comparison;
        SupportsNegation = supportsNegation;
        Phases = phases;
    }

    /// <summary>Gets the metadata-backed source column.</summary>
    public SourceColumnRef Column { get; }

    /// <summary>Gets the supported match shapes.</summary>
    public SourceStringMatchOperations Operations { get; }

    /// <summary>Gets the supported comparison semantics.</summary>
    public SourceStringComparison Comparison { get; }

    /// <summary>Gets whether negated matches are supported.</summary>
    public bool SupportsNegation { get; }

    /// <summary>Gets the phases at which the source can apply matches.</summary>
    public SourcePredicateEvaluationPhases Phases { get; }

    /// <summary>Determines whether this declaration supports a predicate at a specific phase.</summary>
    /// <param name="predicate">The typed predicate.</param>
    /// <param name="phase">The requested application phase.</param>
    /// <returns><see langword="true"/> when all advertised dimensions match; otherwise <see langword="false"/>.</returns>
    public bool Supports(
        SourcePredicateStringMatch predicate,
        SourcePredicateEvaluationPhase phase)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var phaseFlag = phase switch
        {
            SourcePredicateEvaluationPhase.RowFiltering => SourcePredicateEvaluationPhases.RowFiltering,
            SourcePredicateEvaluationPhase.CandidateMetadata => SourcePredicateEvaluationPhases.CandidateMetadata,
            _ => SourcePredicateEvaluationPhases.None
        };

        return phaseFlag != SourcePredicateEvaluationPhases.None &&
               (Phases & phaseFlag) == phaseFlag &&
               string.Equals(Column.Name, predicate.Column.Name, StringComparison.OrdinalIgnoreCase) &&
               Comparison == predicate.Comparison &&
               (!predicate.IsNegated || SupportsNegation) &&
               (Operations & ToOperation(predicate.Kind)) != 0;
    }

    private static SourceStringMatchOperations ToOperation(SourceStringMatchKind kind)
    {
        return kind switch
        {
            SourceStringMatchKind.Exact => SourceStringMatchOperations.Exact,
            SourceStringMatchKind.Prefix => SourceStringMatchOperations.Prefix,
            SourceStringMatchKind.Suffix => SourceStringMatchOperations.Suffix,
            SourceStringMatchKind.Contains => SourceStringMatchOperations.Contains,
            _ => SourceStringMatchOperations.None
        };
    }
}
