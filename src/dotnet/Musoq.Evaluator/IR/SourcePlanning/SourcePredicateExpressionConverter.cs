using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static class SourcePredicateExpressionConverter
{
    public static bool TryConvertPredicate(
        IrExpression expression,
        string sourceAlias,
        [NotNullWhen(true)] out SourcePredicateExpression? predicate)
    {
        switch (expression)
        {
            case ColumnRef columnRef when string.Equals(columnRef.Alias, sourceAlias, StringComparison.OrdinalIgnoreCase):
                predicate = new SourcePredicateColumn(new SourceColumnRef(columnRef.ColumnName));
                return true;
            case Literal literal:
                predicate = new SourcePredicateLiteral(literal.Value);
                return true;
            case BinaryOp { Kind: BinaryOpKind.And or BinaryOpKind.Or } logical:
                if (TryConvertPredicate(logical.Left, sourceAlias, out var leftLogical) &&
                    TryConvertPredicate(logical.Right, sourceAlias, out var rightLogical))
                {
                    predicate = new SourcePredicateLogical(
                        logical.Kind == BinaryOpKind.And
                            ? SourcePredicateLogicalOperator.And
                            : SourcePredicateLogicalOperator.Or,
                        leftLogical,
                        rightLogical);
                    return true;
                }

                break;
            case BinaryOp comparison when TryConvertComparisonOperator(comparison.Kind, out var comparisonOperator):
                if (TryConvertPredicate(comparison.Left, sourceAlias, out var leftComparison) &&
                    TryConvertPredicate(comparison.Right, sourceAlias, out var rightComparison))
                {
                    predicate = new SourcePredicateComparison(
                        comparisonOperator,
                        leftComparison,
                        rightComparison);
                    return true;
                }

                break;
            case InCheck inCheck:
                if (TryConvertPredicate(inCheck.Expression, sourceAlias, out var inExpression))
                {
                    var values = new List<SourcePredicateExpression>(inCheck.Values.Count);
                    foreach (var value in inCheck.Values)
                    {
                        if (!TryConvertPredicate(value, sourceAlias, out var convertedValue))
                        {
                            predicate = null;
                            return false;
                        }

                        values.Add(convertedValue);
                    }

                    predicate = new SourcePredicateIn(inExpression, values);
                    return true;
                }

                break;
            case IsNullCheck nullCheck:
                if (TryConvertPredicate(nullCheck.Expression, sourceAlias, out var nullExpression))
                {
                    predicate = new SourcePredicateNullCheck(nullExpression, nullCheck.IsNegated);
                    return true;
                }

                break;
        }

        predicate = null;
        return false;
    }

    private static bool TryConvertComparisonOperator(
        BinaryOpKind kind,
        out SourcePredicateComparisonOperator comparisonOperator)
    {
        switch (kind)
        {
            case BinaryOpKind.Equal:
                comparisonOperator = SourcePredicateComparisonOperator.Equal;
                return true;
            case BinaryOpKind.NotEqual:
                comparisonOperator = SourcePredicateComparisonOperator.NotEqual;
                return true;
            case BinaryOpKind.GreaterThan:
                comparisonOperator = SourcePredicateComparisonOperator.GreaterThan;
                return true;
            case BinaryOpKind.GreaterOrEqual:
                comparisonOperator = SourcePredicateComparisonOperator.GreaterOrEqual;
                return true;
            case BinaryOpKind.LessThan:
                comparisonOperator = SourcePredicateComparisonOperator.LessThan;
                return true;
            case BinaryOpKind.LessOrEqual:
                comparisonOperator = SourcePredicateComparisonOperator.LessOrEqual;
                return true;
            default:
                comparisonOperator = default;
                return false;
        }
    }
}
