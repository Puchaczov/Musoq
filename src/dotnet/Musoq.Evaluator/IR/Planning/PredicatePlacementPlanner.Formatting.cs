using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;
namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static PlanningDecision CreateDecision(PredicatePlacementPlan plan)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.PredicatePlacement,
            "PredicatePlacementPlan",
            plan.PredicateId,
            plan.EarliestPlacement.ToString(),
            plan.Confidence,
            plan.Reason);
    }

    private static string FormatApplyName(ApplyKind applyKind)
    {
        return applyKind == ApplyKind.Outer ? "Outer APPLY" : "Cross APPLY";
    }

    private static IEnumerable<IrExpression> SplitConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            foreach (var left in SplitConjuncts(and.Left))
                yield return left;

            foreach (var right in SplitConjuncts(and.Right))
                yield return right;

            yield break;
        }

        yield return predicate;
    }

    private static string[] ExtractAliases(IrExpression predicate)
    {
        return AliasRefExtractor.Extract(predicate).ToArray();
    }

    private static string CreatePredicateId(PredicatePlacementOrigin origin, int index)
    {
        return $"{origin}:{index}";
    }

    private static string FormatPlacement(PredicateEarliestPlacement placement)
    {
        return placement switch
        {
            PredicateEarliestPlacement.PreInnerJoinLeft => "pre-inner-join left",
            PredicateEarliestPlacement.PreInnerJoinRight => "pre-inner-join right",
            PredicateEarliestPlacement.PostJoin => "post-join",
            PredicateEarliestPlacement.PostAggregate => "post-aggregate",
            PredicateEarliestPlacement.PostWindow => "post-window",
            _ => placement.ToString()
        };
    }
}
