using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateConjunctMatcher
{
    public static IReadOnlyList<IrExpression> MatchAcceptedConjuncts(
        SourcePredicateExpression acceptedPredicate,
        SourcePredicatePlan sourcePredicatePlan,
        bool allowWholePredicateMatch = false)
    {
        if (!TryCollectSourceAndConjuncts(acceptedPredicate, out var acceptedSourceConjuncts))
        {
            if (!allowWholePredicateMatch)
                return [];

            return MatchWholeAcceptedPredicate(acceptedPredicate, sourcePredicatePlan);
        }

        var acceptedPredicates = new List<IrExpression>();
        foreach (var pushedPredicate in sourcePredicatePlan.PushedPredicates)
        {
            if (!SourcePredicateExpressionConverter.TryConvertPredicate(
                    pushedPredicate,
                    sourcePredicatePlan.Alias,
                    out var sourcePredicate))
            {
                continue;
            }

            if (acceptedSourceConjuncts.Any(accepted =>
                    SourcePredicateExpressionComparer.Instance.Equals(accepted, sourcePredicate)))
            {
                acceptedPredicates.Add(pushedPredicate);
            }
        }

        return acceptedPredicates;
    }

    public static IrExpression? RemoveAcceptedConjuncts(
        IrExpression predicate,
        IReadOnlyList<IrExpression> acceptedPredicates)
    {
        var remainingAccepted = acceptedPredicates.ToList();
        var remainingConjuncts = new List<IrExpression>();

        foreach (var conjunct in SplitIrAndConjuncts(predicate))
        {
            var acceptedIndex = remainingAccepted.FindIndex(accepted => Equals(accepted, conjunct));
            if (acceptedIndex >= 0)
            {
                remainingAccepted.RemoveAt(acceptedIndex);
                continue;
            }

            remainingConjuncts.Add(conjunct);
        }

        if (remainingConjuncts.Count == 0)
            return null;

        if (remainingConjuncts.Count == 1)
            return remainingConjuncts[0];

        var result = remainingConjuncts[0];
        for (var index = 1; index < remainingConjuncts.Count; index++)
            result = new BinaryOp(BinaryOpKind.And, result, remainingConjuncts[index], typeof(bool));

        return result;
    }

    public static IReadOnlyList<IrExpression> SplitIrAndConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
            return SplitIrAndConjuncts(and.Left).Concat(SplitIrAndConjuncts(and.Right)).ToArray();

        return [predicate];
    }

}
