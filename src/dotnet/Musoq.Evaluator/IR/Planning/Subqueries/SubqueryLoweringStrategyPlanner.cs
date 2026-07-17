using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal static class SubqueryLoweringStrategyPlanner
{
    public static SubqueryLoweringStrategyPlanningResult Plan(PhysicalNode physicalPlan)
    {
        var definitions = new Dictionary<string, PhysicalCteDefinition>(StringComparer.OrdinalIgnoreCase);
        var strategies = new Dictionary<string, SubqueryLoweringStrategyDecision>(StringComparer.OrdinalIgnoreCase);

        CollectDefinitions(physicalPlan, definitions);
        Visit(physicalPlan, definitions, strategies);

        var ordered = strategies.Values
            .OrderBy(static strategy => strategy.CteName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new SubqueryLoweringStrategyPlanningResult(ordered, ordered.Select(CreateDecision).ToArray());
    }

    private static void CollectDefinitions(
        PhysicalNode node,
        Dictionary<string, PhysicalCteDefinition> definitions)
    {
        if (node is PhysicalCteNode cte)
        {
            foreach (var definition in cte.Definitions)
            {
                definitions[definition.Name] = definition;
                CollectDefinitions(definition.Plan, definitions);
            }

            CollectDefinitions(cte.Query, definitions);
            return;
        }

        foreach (var child in node.Children)
            CollectDefinitions(child, definitions);
    }

    private static void Visit(
        PhysicalNode node,
        IReadOnlyDictionary<string, PhysicalCteDefinition> definitions,
        Dictionary<string, SubqueryLoweringStrategyDecision> strategies)
    {
        switch (node)
        {
            case PhysicalHashJoinNode hashJoin:
                RecordJoin(hashJoin.Kind, hashJoin.Left, hashJoin.Right, usesRangeIndex: false, definitions, strategies);
                break;
            case PhysicalNestedLoopJoinNode nestedJoin:
                RecordJoin(nestedJoin.Kind, nestedJoin.Left, nestedJoin.Right, usesRangeIndex: false, definitions, strategies);
                break;
            case PhysicalSortMergeJoinNode sortMergeJoin:
                RecordJoin(sortMergeJoin.Kind, sortMergeJoin.Left, sortMergeJoin.Right, usesRangeIndex: true, definitions, strategies);
                break;
            case PhysicalCteRefNode cteRef:
                RecordCteRef(cteRef, null, usesRangeIndex: false, definitions, strategies);
                break;
        }

        foreach (var child in node.Children)
            Visit(child, definitions, strategies);
    }

    private static void RecordJoin(
        JoinKind kind,
        PhysicalNode left,
        PhysicalNode right,
        bool usesRangeIndex,
        IReadOnlyDictionary<string, PhysicalCteDefinition> definitions,
        Dictionary<string, SubqueryLoweringStrategyDecision> strategies)
    {
        if (TryGetBoundaryCteRef(left, out var leftCte))
            RecordCteRef(leftCte, kind, usesRangeIndex, definitions, strategies);

        if (TryGetBoundaryCteRef(right, out var rightCte))
            RecordCteRef(rightCte, kind, usesRangeIndex, definitions, strategies);
    }

    private static void RecordCteRef(
        PhysicalCteRefNode cteRef,
        JoinKind? joinKind,
        bool usesRangeIndex,
        IReadOnlyDictionary<string, PhysicalCteDefinition> definitions,
        Dictionary<string, SubqueryLoweringStrategyDecision> strategies)
    {
        if (!IsGeneratedSubqueryCte(cteRef.CteName) || strategies.ContainsKey(cteRef.CteName))
            return;

        definitions.TryGetValue(cteRef.CteName, out var definition);
        strategies[cteRef.CteName] = new SubqueryLoweringStrategyDecision(
            cteRef.CteName,
            Classify(cteRef.CteName, joinKind, usesRangeIndex, definition),
            joinKind,
            IsCorrelated(definition),
            CreateReason(cteRef.CteName, joinKind, usesRangeIndex, definition));
    }

    private static bool TryGetBoundaryCteRef(PhysicalNode node, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PhysicalCteRefNode? cteRef)
    {
        switch (node)
        {
            case PhysicalCteRefNode direct:
                cteRef = direct;
                return true;
            case PhysicalFilterNode filter:
                return TryGetBoundaryCteRef(filter.Input, out cteRef);
            case PhysicalProjectNode project:
                return TryGetBoundaryCteRef(project.Input, out cteRef);
            default:
                cteRef = null;
                return false;
        }
    }

    private static SubqueryLoweringKind Classify(
        string cteName,
        JoinKind? joinKind,
        bool usesRangeIndex,
        PhysicalCteDefinition? definition)
    {
        if (GeneratedSubqueryContract.IsDerivedTableCteName(cteName))
            return joinKind.HasValue ? SubqueryLoweringKind.DerivedTableJoin : SubqueryLoweringKind.DerivedTableScan;

        if (HasColumn(definition, GeneratedSubqueryContract.CreateValueColumnName(cteName)))
        {
            if (joinKind == JoinKind.LeftSingle && usesRangeIndex)
                return SubqueryLoweringKind.ScalarRangeSingle;

            return joinKind == JoinKind.LeftSingle
                ? SubqueryLoweringKind.ScalarHashSingle
                : SubqueryLoweringKind.ScalarLeftJoin;
        }

        return joinKind switch
        {
            JoinKind.LeftSemi => usesRangeIndex
                ? SubqueryLoweringKind.PredicateRangeSemiJoin
                : SubqueryLoweringKind.PredicateSemiJoin,
            JoinKind.LeftAntiSemi => usesRangeIndex
                ? SubqueryLoweringKind.PredicateRangeAntiSemiJoin
                : SubqueryLoweringKind.PredicateAntiSemiJoin,
            JoinKind.LeftMark => usesRangeIndex
                ? SubqueryLoweringKind.PredicateRangeMark
                : SubqueryLoweringKind.PredicateHashMark,
            _ => SubqueryLoweringKind.PredicateCte
        };
    }

    private static PlanningDecision CreateDecision(SubqueryLoweringStrategyDecision strategy)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.SubqueryStrategy,
            "SubqueryLoweringStrategy",
            strategy.CteName,
            strategy.Kind.ToString(),
            PlanningConfidence.High,
            strategy.Reason);
    }

    private static string CreateReason(
        string cteName,
        JoinKind? joinKind,
        bool usesRangeIndex,
        PhysicalCteDefinition? definition)
    {
        var correlation = IsCorrelated(definition) ? "correlated" : "uncorrelated";
        var joinText = joinKind.HasValue
            ? $" using {joinKind.Value} {(usesRangeIndex ? "range-index" : "join")} lowering"
            : " as a materialized CTE source";
        return $"Generated {correlation} subquery CTE '{cteName}' is planned{joinText}.";
    }

    private static bool IsGeneratedSubqueryCte(string cteName)
    {
        return GeneratedSubqueryContract.IsGeneratedSubqueryCteName(cteName);
    }

    private static bool IsCorrelated(PhysicalCteDefinition? definition)
    {
        return definition?.Plan.OutputSchema.Columns.Any(static column =>
            GeneratedSubqueryContract.IsCorrelationColumnName(column.Name)) == true;
    }

    private static bool HasColumn(PhysicalCteDefinition? definition, string columnName)
    {
        return definition?.Plan.OutputSchema.FindByName(columnName) != null;
    }
}
