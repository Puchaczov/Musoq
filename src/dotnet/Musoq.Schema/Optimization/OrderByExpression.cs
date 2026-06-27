namespace Musoq.Schema.Optimization;

public sealed record OrderByExpression(SourceColumnRef Column, OrderDirection Direction);
