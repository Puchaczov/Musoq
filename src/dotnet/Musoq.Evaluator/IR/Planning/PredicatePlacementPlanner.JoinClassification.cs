using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static PredicatePlacementPlan ClassifyJoinPredicate(
        JoinNode join,
        IrExpression predicate,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        int index)
    {
        if (join.Kind == JoinKind.Inner)
        {
            return ClassifyInnerJoinPredicate(
                predicate,
                join,
                sourcesByAlias,
                index);
        }

        return CreatePlan(
            PredicatePlacementOrigin.JoinOn,
            index,
            predicate,
            PredicateEarliestPlacement.PostJoin,
            PlanningConfidence.Medium,
            CreateJoinBoundaryReason(join.Kind));
    }

    private static PredicatePlacementPlan ClassifyInnerJoinPredicate(
        IrExpression predicate,
        JoinNode join,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        int index)
    {
        var aliases = ExtractAliases(predicate);

        if (aliases.Length == 0)
        {
            return CreatePlan(
                PredicatePlacementOrigin.JoinOn,
                index,
                predicate,
                PredicateEarliestPlacement.ConstantPredicate,
                PlanningConfidence.High,
                "Join predicate does not reference source aliases and can be treated as a constant predicate.");
        }

        if (aliases.Length == 1 && HasAmbiguousSourceAlias(sourcesByAlias, aliases[0]))
        {
            return CreatePlan(
                PredicatePlacementOrigin.JoinOn,
                index,
                predicate,
                PredicateEarliestPlacement.RuntimeFilter,
                PlanningConfidence.Low,
                "Join predicate alias appears in multiple source scopes, so placement remains conservative.");
        }

        var leftAliases = CollectProducedAliases(join.Left);
        if (IsSubset(aliases, leftAliases))
        {
            return CreatePlan(
                PredicatePlacementOrigin.JoinOn,
                index,
                predicate,
                PredicateEarliestPlacement.PreInnerJoinLeft,
                PlanningConfidence.High,
                "Inner join predicate references only left input alias(es), so it is eligible before the inner join left boundary. Physical movement is not applied in this diagnostics-only wave.");
        }

        var rightAliases = CollectProducedAliases(join.Right);
        if (IsSubset(aliases, rightAliases))
        {
            return CreatePlan(
                PredicatePlacementOrigin.JoinOn,
                index,
                predicate,
                PredicateEarliestPlacement.PreInnerJoinRight,
                PlanningConfidence.High,
                "Inner join predicate references only right input alias(es), so it is eligible before the inner join right boundary. Physical movement is not applied in this diagnostics-only wave.");
        }

        if (aliases.Any(leftAliases.Contains) && aliases.Any(rightAliases.Contains))
        {
            return CreatePlan(
                PredicatePlacementOrigin.JoinOn,
                index,
                predicate,
                PredicateEarliestPlacement.PostJoin,
                PlanningConfidence.High,
                "Inner join predicate references both inputs and must wait until the post-join row is available.");
        }

        return CreatePlan(
            PredicatePlacementOrigin.JoinOn,
            index,
            predicate,
            PredicateEarliestPlacement.RuntimeFilter,
            PlanningConfidence.Low,
            "Inner join predicate aliases could not be matched to a planned join input.");
    }

}
