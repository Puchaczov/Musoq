using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateConjunctMatcher
{
    private static IReadOnlyList<IrExpression> MatchWholeAcceptedPredicate(
        SourcePredicateExpression acceptedPredicate,
        SourcePredicatePlan sourcePredicatePlan)
    {
        foreach (var pushedPredicate in sourcePredicatePlan.PushedPredicates)
        {
            if (!SourcePredicateExpressionConverter.TryConvertPredicate(
                    pushedPredicate,
                    sourcePredicatePlan.Alias,
                    out var sourcePredicate))
            {
                continue;
            }

            if (SourcePredicateExpressionComparer.Instance.Equals(acceptedPredicate, sourcePredicate))
                return [pushedPredicate];
        }

        return [];
    }
}
