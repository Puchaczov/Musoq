namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateNullCheck(
    SourcePredicateExpression Expression,
    bool IsNegated = false) : SourcePredicateExpression;
