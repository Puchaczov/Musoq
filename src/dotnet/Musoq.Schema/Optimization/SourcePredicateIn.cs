using System.Collections.Generic;
using System.Linq;

namespace Musoq.Schema.Optimization;

public sealed record SourcePredicateIn : SourcePredicateExpression
{
    public SourcePredicateIn(
        SourcePredicateExpression expression,
        IReadOnlyList<SourcePredicateExpression> values,
        bool IsNegated = false)
    {
        Expression = expression;
        Values = Array.AsReadOnly(values.ToArray());
        this.IsNegated = IsNegated;
    }

    public SourcePredicateExpression Expression { get; init; }

    public IReadOnlyList<SourcePredicateExpression> Values { get; init; }

    public bool IsNegated { get; init; }

    public void Deconstruct(
        out SourcePredicateExpression expression,
        out IReadOnlyList<SourcePredicateExpression> values,
        out bool IsNegated)
    {
        expression = Expression;
        values = Values;
        IsNegated = this.IsNegated;
    }
}
