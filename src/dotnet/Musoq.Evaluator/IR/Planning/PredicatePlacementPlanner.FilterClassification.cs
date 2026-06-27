using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;
namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static PredicatePlacementPlan ClassifyFilterPredicate(
        IrExpression predicate,
        PredicatePlacementOrigin origin,
        LogicalNode input,
        IReadOnlyList<LogicalNode> ancestors,
        IReadOnlyList<JoinNode> joinBoundaries,
        IReadOnlyList<ApplyNode> applyBoundaries,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        IReadOnlyDictionary<string, IReadOnlySet<string>> pushedPredicatesBySourceId,
        int index)
    {
        var aliases = ExtractAliases(predicate);
        var predicateText = IrExpressionPrinter.Print(predicate);

        if (aliases.Length == 0)
        {
            return CreatePlan(
                origin,
                index,
                predicate,
                PredicateEarliestPlacement.ConstantPredicate,
                PlanningConfidence.High,
                "Predicate does not reference source aliases and can be treated as a constant predicate.");
        }

        if (TryClassifyPostJoinFilter(input, ancestors, joinBoundaries, origin, index, predicate, aliases) is { } postJoinPlan)
            return postJoinPlan;

        if (TryClassifyApplyFilter(input, applyBoundaries, origin, index, predicate, aliases) is { } applyPlan)
            return applyPlan;

        if (aliases.Length == 1 && TryResolveUniqueSource(sourcesByAlias, aliases[0]) is { } source)
        {
            if (IsPushedPredicate(source.SourceContextId, predicateText, pushedPredicatesBySourceId))
            {
                return CreatePlan(
                    origin,
                    index,
                    predicate,
                    PredicateEarliestPlacement.SourcePushdown,
                    PlanningConfidence.High,
                    "Predicate is source-local and has a planner-owned source pushdown plan.");
            }

            return CreatePlan(
                origin,
                index,
                predicate,
                PredicateEarliestPlacement.SourceRuntimeFilter,
                PlanningConfidence.Medium,
                "Predicate is source-local but remains a runtime filter under the current physical shape.");
        }

        if (aliases.Length == 1 && HasAmbiguousSourceAlias(sourcesByAlias, aliases[0]))
        {
            return CreatePlan(
                origin,
                index,
                predicate,
                PredicateEarliestPlacement.RuntimeFilter,
                PlanningConfidence.Low,
                "Predicate alias appears in multiple source scopes, so placement remains conservative.");
        }

        if (aliases.Length > 1)
        {
            return CreatePlan(
                origin,
                index,
                predicate,
                PredicateEarliestPlacement.PostJoin,
                PlanningConfidence.High,
                "Predicate references multiple aliases and must wait until a combined row is available.");
        }

        return CreatePlan(
            origin,
            index,
            predicate,
            PredicateEarliestPlacement.RuntimeFilter,
            PlanningConfidence.Low,
            "Predicate alias could not be matched to a planned source.");
    }

    private static PredicatePlacementPlan? TryClassifyPostJoinFilter(
        LogicalNode input,
        IReadOnlyList<LogicalNode> ancestors,
        IReadOnlyList<JoinNode> joinBoundaries,
        PredicatePlacementOrigin origin,
        int index,
        IrExpression predicate,
        string[] aliases)
    {
        var join = FindJoinBoundary(input) ??
                   FindNearestJoinBoundary(ancestors) ??
                   FindRelatedJoinBoundary(joinBoundaries, aliases);
        if (join is null)
            return null;

        if (join.Kind != JoinKind.Inner)
        {
            return CreatePlan(
                origin,
                index,
                predicate,
                PredicateEarliestPlacement.PostJoin,
                PlanningConfidence.Medium,
                CreateJoinBoundaryReason(join.Kind));
        }

        if (aliases.Length <= 1)
            return null;

        return CreatePlan(
            origin,
            index,
            predicate,
            PredicateEarliestPlacement.PostJoin,
            PlanningConfidence.High,
            "WHERE predicate references multiple inner-join aliases and must wait until the post-join row is available.");
    }

    private static PredicatePlacementPlan? TryClassifyApplyFilter(
        LogicalNode input,
        IReadOnlyList<ApplyNode> applyBoundaries,
        PredicatePlacementOrigin origin,
        int index,
        IrExpression predicate,
        string[] aliases)
    {
        if (aliases.Length <= 1)
            return null;

        var apply = input as ApplyNode ??
                    FindApplyBoundary(input) ??
                    FindRelatedApplyBoundary(applyBoundaries, aliases);
        if (apply is null)
            return null;

        var leftAliases = CollectProducedAliases(apply.Left);
        var rightAliases = CollectProducedAliases(apply.Right);
        if (!aliases.Any(leftAliases.Contains) || !aliases.Any(rightAliases.Contains))
            return null;

        return CreatePlan(
            origin,
            index,
            predicate,
            PredicateEarliestPlacement.RuntimeFilter,
            PlanningConfidence.Medium,
            $"Predicate crosses a {FormatApplyName(apply.Kind)} boundary, so placement remains runtime-only until APPLY correlation movement is implemented.");
    }
}
