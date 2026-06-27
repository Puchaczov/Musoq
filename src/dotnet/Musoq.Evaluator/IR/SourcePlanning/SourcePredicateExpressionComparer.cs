using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal sealed class SourcePredicateExpressionComparer : IEqualityComparer<SourcePredicateExpression>
{
    public static SourcePredicateExpressionComparer Instance { get; } = new();

    private SourcePredicateExpressionComparer()
    {
    }

    public bool Equals(SourcePredicateExpression? left, SourcePredicateExpression? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        return left switch
        {
            SourcePredicateColumn leftColumn when right is SourcePredicateColumn rightColumn =>
                string.Equals(leftColumn.Column.Name, rightColumn.Column.Name, StringComparison.OrdinalIgnoreCase),
            SourcePredicateLiteral leftLiteral when right is SourcePredicateLiteral rightLiteral =>
                Equals(leftLiteral.Value, rightLiteral.Value),
            SourcePredicateComparison leftComparison when right is SourcePredicateComparison rightComparison =>
                leftComparison.Operator == rightComparison.Operator &&
                Equals(leftComparison.Left, rightComparison.Left) &&
                Equals(leftComparison.Right, rightComparison.Right),
            SourcePredicateLogical leftLogical when right is SourcePredicateLogical rightLogical =>
                leftLogical.Operator == rightLogical.Operator &&
                Equals(leftLogical.Left, rightLogical.Left) &&
                Equals(leftLogical.Right, rightLogical.Right),
            SourcePredicateIn leftIn when right is SourcePredicateIn rightIn =>
                leftIn.IsNegated == rightIn.IsNegated &&
                Equals(leftIn.Expression, rightIn.Expression) &&
                leftIn.Values.Count == rightIn.Values.Count &&
                leftIn.Values.Zip(rightIn.Values, Equals).All(static equals => equals),
            SourcePredicateNullCheck leftNull when right is SourcePredicateNullCheck rightNull =>
                leftNull.IsNegated == rightNull.IsNegated &&
                Equals(leftNull.Expression, rightNull.Expression),
            _ => false
        };
    }

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
        }
    }
}
