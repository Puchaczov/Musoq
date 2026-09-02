namespace Musoq.Schema.Optimization;

/// <summary>
///     Represents an explicit flags-helper predicate offered to a datasource.
/// </summary>
public sealed record SourcePredicateFlags(
    SourcePredicateExpression Expression,
    SourcePredicateEnumLiteral Mask,
    SourcePredicateFlagsMatchMode MatchMode) : SourcePredicateExpression;
