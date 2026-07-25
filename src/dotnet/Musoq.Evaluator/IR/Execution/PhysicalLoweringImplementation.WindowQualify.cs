using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static WindowQualifyTopRankPlan CreateWindowQualifyTopRankPlan(
        IrExpression? qualifyPredicate,
        IReadOnlyList<WindowRegistrationBuildResult> registrations)
    {
        if (qualifyPredicate == null)
            return new WindowQualifyTopRankPlan(null, new Dictionary<int, long>());

        var rankingIndexes = registrations
            .Where(static registration => registration.RankingFunction != null)
            .Select(static registration => registration.Registration!.WindowIndex)
            .ToHashSet();

        if (rankingIndexes.Count == 0)
            return new WindowQualifyTopRankPlan(qualifyPredicate, new Dictionary<int, long>());

        var upperBounds = new Dictionary<int, long>();
        var predicate = RewriteTopRankQualifyPredicate(qualifyPredicate, rankingIndexes, upperBounds);
        return new WindowQualifyTopRankPlan(predicate, upperBounds);
    }

    private static IrExpression RewriteTopRankQualifyPredicate(
        IrExpression predicate,
        IReadOnlySet<int> rankingIndexes,
        Dictionary<int, long> upperBounds)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } binaryAnd)
        {
            var left = RewriteTopRankQualifyPredicate(binaryAnd.Left, rankingIndexes, upperBounds);
            var right = RewriteTopRankQualifyPredicate(binaryAnd.Right, rankingIndexes, upperBounds);
            return ReferenceEquals(left, binaryAnd.Left) && ReferenceEquals(right, binaryAnd.Right)
                ? predicate
                : binaryAnd with { Left = left, Right = right };
        }

        if (!TryGetTopRankUpperBound(predicate, rankingIndexes, out var windowRef, out var upperBound))
            return predicate;

        if (upperBounds.TryGetValue(windowRef.WindowIndex, out var existingUpperBound))
            upperBounds[windowRef.WindowIndex] = Math.Min(existingUpperBound, upperBound);
        else
            upperBounds.Add(windowRef.WindowIndex, upperBound);

        var positiveRank = new BinaryOp(
            BinaryOpKind.GreaterThan,
            windowRef,
            new Literal(0L, typeof(long)),
            typeof(bool));

        return new BinaryOp(
            BinaryOpKind.And,
            positiveRank,
            predicate,
            typeof(bool));
    }

    private static bool TryGetTopRankUpperBound(
        IrExpression predicate,
        IReadOnlySet<int> rankingIndexes,
        out WindowFunctionRef windowRef,
        out long upperBound)
    {
        windowRef = null!;
        upperBound = 0;

        if (predicate is not BinaryOp binary)
            return false;

        if (TryGetIntegralLiteral(binary.Right, out var rightValue) &&
            binary.Left is WindowFunctionRef leftWindow &&
            rankingIndexes.Contains(leftWindow.WindowIndex))
        {
            return TryCreateLeftWindowUpperBound(binary.Kind, leftWindow, rightValue, out windowRef, out upperBound);
        }

        if (TryGetIntegralLiteral(binary.Left, out var leftValue) &&
            binary.Right is WindowFunctionRef rightWindow &&
            rankingIndexes.Contains(rightWindow.WindowIndex))
        {
            return TryCreateRightWindowUpperBound(binary.Kind, rightWindow, leftValue, out windowRef, out upperBound);
        }

        return false;
    }

    private static bool TryCreateLeftWindowUpperBound(
        BinaryOpKind kind,
        WindowFunctionRef candidate,
        long literal,
        out WindowFunctionRef windowRef,
        out long upperBound)
    {
        windowRef = candidate;
        upperBound = kind switch
        {
            BinaryOpKind.LessOrEqual => literal,
            BinaryOpKind.LessThan => literal - 1,
            _ => long.MinValue
        };

        return upperBound != long.MinValue;
    }

    private static bool TryCreateRightWindowUpperBound(
        BinaryOpKind kind,
        WindowFunctionRef candidate,
        long literal,
        out WindowFunctionRef windowRef,
        out long upperBound)
    {
        windowRef = candidate;
        upperBound = kind switch
        {
            BinaryOpKind.GreaterOrEqual => literal,
            BinaryOpKind.GreaterThan => literal - 1,
            _ => long.MinValue
        };

        return upperBound != long.MinValue;
    }

    private static bool TryGetIntegralLiteral(IrExpression expression, out long value)
    {
        value = 0;
        if (expression is not Literal literal || literal.Value == null)
            return false;

        try
        {
            value = Convert.ToInt64(literal.Value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }
}
