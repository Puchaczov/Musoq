using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateLiteral(object? Value) : SourcePredicateExpression;
