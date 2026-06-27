using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    private static SortMergeJoinDecomposition? TryDecomposeSortMergeJoin(
        JoinKind kind,
        IrExpression predicate,
        PhysicalNode left,
        PhysicalNode right)
    {
        if (kind != JoinKind.Inner || predicate == null)
            return null;

        var leftAliases = CollectAliases(left);
        var rightAliases = CollectAliases(right);

        return TryFindSortMergeCondition(predicate, leftAliases, rightAliases, out var key)
            ? new SortMergeJoinDecomposition(
                key.LeftKey,
                key.RightKey,
                key.ComparisonKind,
                predicate)
            : null;
    }

    private static bool TryFindSortMergeCondition(
        IrExpression expression,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases,
        out NormalizedSortMergeKey key)
    {
        if (expression is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            if (TryFindSortMergeCondition(and.Left, leftAliases, rightAliases, out key))
                return true;

            return TryFindSortMergeCondition(and.Right, leftAliases, rightAliases, out key);
        }

        if (expression is BinaryOp range &&
            IsSortMergeRangeKind(range.Kind) &&
            TryNormalizeSortMergeRange(range, leftAliases, rightAliases, out key))
        {
            return true;
        }

        key = default;
        return false;
    }

    private static bool IsSortMergeRangeKind(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.GreaterThan
            or BinaryOpKind.GreaterOrEqual
            or BinaryOpKind.LessThan
            or BinaryOpKind.LessOrEqual;
    }

    private static bool TryNormalizeSortMergeRange(
        BinaryOp range,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases,
        out NormalizedSortMergeKey key)
    {
        var leftSide = ClassifyJoinSide(range.Left, leftAliases, rightAliases);
        var rightSide = ClassifyJoinSide(range.Right, leftAliases, rightAliases);

        if (leftSide == JoinExpressionSide.Left && rightSide == JoinExpressionSide.Right &&
            CanCompareSortMergeKeys(range.Left.ReturnType, range.Right.ReturnType))
        {
            key = new NormalizedSortMergeKey(range.Left, range.Right, range.Kind);
            return true;
        }

        if (leftSide == JoinExpressionSide.Right && rightSide == JoinExpressionSide.Left &&
            CanCompareSortMergeKeys(range.Left.ReturnType, range.Right.ReturnType))
        {
            key = new NormalizedSortMergeKey(
                range.Right,
                range.Left,
                ReverseComparisonKind(range.Kind));
            return true;
        }

        key = default;
        return false;
    }

    private static JoinExpressionSide ClassifyJoinSide(
        IrExpression expression,
        HashSet<string> leftAliases,
        HashSet<string> rightAliases)
    {
        var columns = ColumnRefExtractor.Extract(expression);
        var hasLeft = ReferencesAliases(columns, leftAliases);
        var hasRight = ReferencesAliases(columns, rightAliases);

        return (hasLeft, hasRight) switch
        {
            (true, false) => JoinExpressionSide.Left,
            (false, true) => JoinExpressionSide.Right,
            (false, false) => JoinExpressionSide.Constant,
            _ => JoinExpressionSide.Mixed
        };
    }

    private static bool CanCompareSortMergeKeys(Type leftType, Type rightType)
    {
        leftType = Nullable.GetUnderlyingType(leftType) ?? leftType;
        rightType = Nullable.GetUnderlyingType(rightType) ?? rightType;

        return leftType == rightType &&
               leftType != typeof(object) &&
               typeof(IComparable).IsAssignableFrom(leftType);
    }

    private static BinaryOpKind ReverseComparisonKind(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.GreaterThan => BinaryOpKind.LessThan,
            BinaryOpKind.GreaterOrEqual => BinaryOpKind.LessOrEqual,
            BinaryOpKind.LessThan => BinaryOpKind.GreaterThan,
            BinaryOpKind.LessOrEqual => BinaryOpKind.GreaterOrEqual,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only range comparison kinds can be reversed.")
        };
    }
}
