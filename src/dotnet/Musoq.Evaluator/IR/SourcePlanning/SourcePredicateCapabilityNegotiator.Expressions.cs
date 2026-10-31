using System.Collections.Generic;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static partial class SourcePredicateCapabilityNegotiator
{
    private static SourcePredicateExpression? RemoveTypedPredicates(
        SourcePredicateExpression? predicate,
        out SourcePredicateExpression? deferred)
    {
        if (predicate == null)
        {
            deferred = null;
            return null;
        }

        var accepted = new List<SourcePredicateExpression>();
        var deferredConjuncts = new List<SourcePredicateExpression>();
        foreach (var conjunct in FlattenConjunction(predicate))
        {
            if (SourcePredicatePatternMatchFacts.ContainsPatternMatch(conjunct))
                deferredConjuncts.Add(conjunct);
            else
                accepted.Add(conjunct);
        }

        deferred = Combine(deferredConjuncts);
        return Combine(accepted);
    }

    private static bool SupportsTopLevelPatternMatch(
        SourcePredicateExpression conjunct,
        SourcePredicateCapabilities capabilities)
    {
        return conjunct is SourcePredicatePatternMatch patternMatch &&
               (capabilities.Supports(patternMatch, SourcePredicateEvaluationPhase.RowFiltering) ||
                capabilities.Supports(patternMatch, SourcePredicateEvaluationPhase.CandidateMetadata));
    }
}
