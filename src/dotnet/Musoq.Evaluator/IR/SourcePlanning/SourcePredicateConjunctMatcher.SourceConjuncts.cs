using System.Collections.Generic;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateConjunctMatcher
{
    public static bool TryCollectSourceAndConjuncts(
        SourcePredicateExpression acceptedPredicate,
        out IReadOnlyList<SourcePredicateExpression> conjuncts)
    {
        var result = new List<SourcePredicateExpression>();
        if (!TryAddSourceAndConjuncts(acceptedPredicate, result))
        {
            conjuncts = [];
            return false;
        }

        conjuncts = result;
        return true;
    }

    private static bool TryAddSourceAndConjuncts(
        SourcePredicateExpression predicate,
        ICollection<SourcePredicateExpression> conjuncts)
    {
        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.Or })
            return false;

        if (predicate is SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } and)
        {
            return TryAddSourceAndConjuncts(and.Left, conjuncts) &&
                   TryAddSourceAndConjuncts(and.Right, conjuncts);
        }

        conjuncts.Add(predicate);
        return true;
    }
}
