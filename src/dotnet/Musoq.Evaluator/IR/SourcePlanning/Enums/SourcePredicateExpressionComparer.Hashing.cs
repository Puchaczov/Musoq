namespace Musoq.Evaluator.IR.SourcePlanning;

internal sealed partial class SourcePredicateExpressionComparer
{
    public int GetHashCode(SourcePredicateExpression predicate)
    {
        var hash = new HashCode();
        AddHash(predicate, ref hash);
        return hash.ToHashCode();
    }

    private static void AddHash(SourcePredicateExpression predicate, ref HashCode hash)
    {
        switch (predicate)
        {
            case SourcePredicateColumn column:
                hash.Add(nameof(SourcePredicateColumn));
                hash.Add(column.Column.Name, StringComparer.OrdinalIgnoreCase);
                break;
            case SourcePredicateLiteral literal:
                hash.Add(nameof(SourcePredicateLiteral));
                hash.Add(literal.Value);
                break;
            case SourcePredicateEnumLiteral literal:
                hash.Add(nameof(SourcePredicateEnumLiteral));
                hash.Add(literal.Value);
                hash.Add(literal.EnumFingerprint, StringComparer.Ordinal);
                break;
            case SourcePredicateComparison comparison:
                hash.Add(nameof(SourcePredicateComparison));
                hash.Add(comparison.Operator);
                AddHash(comparison.Left, ref hash);
                AddHash(comparison.Right, ref hash);
                break;
            case SourcePredicateLogical logical:
                hash.Add(nameof(SourcePredicateLogical));
                hash.Add(logical.Operator);
                AddHash(logical.Left, ref hash);
                AddHash(logical.Right, ref hash);
                break;
            case SourcePredicateIn inPredicate:
                hash.Add(nameof(SourcePredicateIn));
                hash.Add(inPredicate.IsNegated);
                AddHash(inPredicate.Expression, ref hash);
                foreach (var value in inPredicate.Values)
                    AddHash(value, ref hash);
                break;
            case SourcePredicateNullCheck nullCheck:
                hash.Add(nameof(SourcePredicateNullCheck));
                hash.Add(nullCheck.IsNegated);
                AddHash(nullCheck.Expression, ref hash);
                break;
            case SourcePredicateFlags flags:
                hash.Add(nameof(SourcePredicateFlags));
                hash.Add(flags.MatchMode);
                AddHash(flags.Expression, ref hash);
                AddHash(flags.Mask, ref hash);
                break;
        }
    }
}
