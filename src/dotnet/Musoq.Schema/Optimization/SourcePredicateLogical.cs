using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateLogical(
    SourcePredicateLogicalOperator Operator,
    SourcePredicateExpression Left,
    SourcePredicateExpression Right) : SourcePredicateExpression;
