using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;
namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    public static PredicatePlacementPlanningResult Plan(
        LogicalNode logicalPlan,
        IReadOnlyDictionary<string, SourcePlanProperties> sources,
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(sourcePredicatePlans);
        var sourcesByAlias = sources.Values
            .GroupBy(static source => source.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var pushedPredicatesBySourceId = CreatePushedPredicateTexts(sourcePredicatePlans);
        var joinBoundaries = CollectJoinBoundaries(logicalPlan);
        var applyBoundaries = CollectApplyBoundaries(logicalPlan);
        var plans = new List<PredicatePlacementPlan>();

        AddPredicatePlacements(logicalPlan, sourcesByAlias, pushedPredicatesBySourceId, joinBoundaries, applyBoundaries, [], plans);

        var decisions = plans
            .Select(CreateDecision)
            .ToArray();

        return new PredicatePlacementPlanningResult(plans, decisions);
    }

    private static Dictionary<string, IReadOnlySet<string>> CreatePushedPredicateTexts(
        IReadOnlyDictionary<string, SourcePredicatePlan> sourcePredicatePlans)
    {
        var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        foreach (var entry in sourcePredicatePlans)
        {
            result[entry.Key] = entry.Value.PushedPredicates
                .Select(IrExpressionPrinter.Print)
                .ToHashSet(StringComparer.Ordinal);
        }

        return result;
    }

    private static void AddPredicatePlacements(
        LogicalNode node,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        IReadOnlyDictionary<string, IReadOnlySet<string>> pushedPredicatesBySourceId,
        IReadOnlyList<JoinNode> joinBoundaries,
        IReadOnlyList<ApplyNode> applyBoundaries,
        IReadOnlyList<LogicalNode> ancestors,
        List<PredicatePlacementPlan> plans)
    {
        switch (node)
        {
            case FilterNode filter:
                AddFilterPlacements(filter, ancestors, joinBoundaries, applyBoundaries, sourcesByAlias, pushedPredicatesBySourceId, plans);
                break;
            case JoinNode join:
                AddJoinPlacements(join, sourcesByAlias, plans);
                break;
            case HavingFilterNode having:
                AddStagePlacements(having.Predicate, PredicatePlacementOrigin.Having, PredicateEarliestPlacement.PostAggregate, plans);
                break;
            case QualifyFilterNode qualify:
                AddStagePlacements(qualify.Predicate, PredicatePlacementOrigin.Qualify, PredicateEarliestPlacement.PostWindow, plans);
                break;
        }

        var childAncestors = AppendAncestor(ancestors, node);
        foreach (var child in node.Children)
            AddPredicatePlacements(child, sourcesByAlias, pushedPredicatesBySourceId, joinBoundaries, applyBoundaries, childAncestors, plans);
    }

    private static void AddFilterPlacements(
        FilterNode filter,
        IReadOnlyList<LogicalNode> ancestors,
        IReadOnlyList<JoinNode> joinBoundaries,
        IReadOnlyList<ApplyNode> applyBoundaries,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        IReadOnlyDictionary<string, IReadOnlySet<string>> pushedPredicatesBySourceId,
        List<PredicatePlacementPlan> plans)
    {
        foreach (var conjunct in SplitConjuncts(filter.Predicate))
        {
            plans.Add(ClassifyFilterPredicate(
                conjunct,
                PredicatePlacementOrigin.Where,
                filter.Input,
                ancestors,
                joinBoundaries,
                applyBoundaries,
                sourcesByAlias,
                pushedPredicatesBySourceId,
                plans.Count));
        }
    }

    private static void AddJoinPlacements(
        JoinNode join,
        IReadOnlyDictionary<string, SourcePlanProperties[]> sourcesByAlias,
        List<PredicatePlacementPlan> plans)
    {
        foreach (var conjunct in SplitConjuncts(join.OnPredicate))
            plans.Add(ClassifyJoinPredicate(join, conjunct, sourcesByAlias, plans.Count));
    }

    private static void AddStagePlacements(
        IrExpression predicate,
        PredicatePlacementOrigin origin,
        PredicateEarliestPlacement placement,
        List<PredicatePlacementPlan> plans)
    {
        foreach (var conjunct in SplitConjuncts(predicate))
        {
            plans.Add(CreatePlan(
                origin,
                plans.Count,
                conjunct,
                placement,
                PlanningConfidence.High,
                $"{origin} predicates are evaluated at their logical {FormatPlacement(placement)} stage."));
        }
    }
}
