namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateColumn(SourceColumnRef Column) : SourcePredicateExpression;
