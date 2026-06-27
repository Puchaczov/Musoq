using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateComparison(
    SourcePredicateComparisonOperator Operator,
    SourcePredicateExpression Left,
    SourcePredicateExpression Right) : SourcePredicateExpression;
