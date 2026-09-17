using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.Optimization;

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
            if (ContainsStringMatch(conjunct))
                deferredConjuncts.Add(conjunct);
            else
                accepted.Add(conjunct);
        }

        deferred = Combine(deferredConjuncts);
        return Combine(accepted);
    }

    private static bool SupportsTopLevelStringMatch(
        SourcePredicateExpression conjunct,
        SourcePredicateCapabilities capabilities)
    {
        return conjunct is SourcePredicateStringMatch stringMatch &&
               (capabilities.Supports(stringMatch, SourcePredicateEvaluationPhase.RowFiltering) ||
                capabilities.Supports(stringMatch, SourcePredicateEvaluationPhase.CandidateMetadata));
    }

    private static bool ContainsStringMatch(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            SourcePredicateStringMatch => true,
            SourcePredicateLogical logical => ContainsStringMatch(logical.Left) || ContainsStringMatch(logical.Right),
            SourcePredicateComparison comparison => ContainsStringMatch(comparison.Left) || ContainsStringMatch(comparison.Right),
            SourcePredicateIn sourceIn => ContainsStringMatch(sourceIn.Expression) ||
                                          sourceIn.Values.Any(ContainsStringMatch),
            SourcePredicateNullCheck nullCheck => ContainsStringMatch(nullCheck.Expression),
            SourcePredicateFlags flags => ContainsStringMatch(flags.Expression) || ContainsStringMatch(flags.Mask),
            _ => false
        };
    }
}
