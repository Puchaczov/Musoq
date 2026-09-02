using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateExpressionConverter
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
                var comparisonEnum = comparison.Left.EnumType ?? comparison.Right.EnumType;
                if (comparisonEnum != null &&
                    comparisonOperator is not (SourcePredicateComparisonOperator.Equal or
                        SourcePredicateComparisonOperator.NotEqual))
                {
                    break;
                }

                if (TryConvertPredicateOperand(comparison.Left, sourceAlias, comparisonEnum, out var leftComparison) &&
                    TryConvertPredicateOperand(comparison.Right, sourceAlias, comparisonEnum, out var rightComparison))
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
                        if (!TryConvertPredicateOperand(
                                value,
                                sourceAlias,
                                inCheck.Expression.EnumType,
                                out var convertedValue))
                        {
                            predicate = null;
                            return false;
                        }

                        values.Add(convertedValue);
                    }

                    predicate = new SourcePredicateIn(inExpression, values, inCheck.IsNegated);
                    return true;
                }

                break;
            case MethodCall methodCall when TryConvertEnumFlagsPredicate(methodCall, sourceAlias, out predicate):
                return true;
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

}
