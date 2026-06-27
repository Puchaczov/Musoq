using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateIn(
    SourcePredicateExpression Expression,
    IReadOnlyList<SourcePredicateExpression> Values,
    bool IsNegated = false) : SourcePredicateExpression;
