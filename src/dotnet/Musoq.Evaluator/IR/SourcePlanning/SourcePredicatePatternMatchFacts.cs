using System.Linq;

namespace Musoq.Evaluator.IR.SourcePlanning;

internal static class SourcePredicatePatternMatchFacts
{
    public static bool ContainsPatternMatch(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            SourcePredicatePatternMatch => true,
            SourcePredicateLogical logical => ContainsPatternMatch(logical.Left) || ContainsPatternMatch(logical.Right),
            SourcePredicateComparison comparison => ContainsPatternMatch(comparison.Left) || ContainsPatternMatch(comparison.Right),
            SourcePredicateIn sourceIn => ContainsPatternMatch(sourceIn.Expression) ||
                                          sourceIn.Values.Any(ContainsPatternMatch),
            SourcePredicateNullCheck nullCheck => ContainsPatternMatch(nullCheck.Expression),
            SourcePredicateFlags flags => ContainsPatternMatch(flags.Expression) || ContainsPatternMatch(flags.Mask),
            _ => false
        };
    }
}
