using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal sealed partial class SourcePredicateExpressionComparer : IEqualityComparer<SourcePredicateExpression>
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
            SourcePredicateEnumLiteral leftLiteral when right is SourcePredicateEnumLiteral rightLiteral =>
                leftLiteral.Value == rightLiteral.Value &&
                string.Equals(leftLiteral.EnumFingerprint, rightLiteral.EnumFingerprint, StringComparison.Ordinal),
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
            SourcePredicateFlags leftFlags when right is SourcePredicateFlags rightFlags =>
                leftFlags.MatchMode == rightFlags.MatchMode &&
                Equals(leftFlags.Expression, rightFlags.Expression) &&
                Equals(leftFlags.Mask, rightFlags.Mask),
            _ => false
        };
    }

}
