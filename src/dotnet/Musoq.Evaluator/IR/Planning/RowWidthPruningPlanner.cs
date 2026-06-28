using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning;

internal static class RowWidthPruningPlanner
{
    public static RowWidthPruningPlanningResult Plan(IReadOnlyList<BoundaryRowShapePlan> rowShapePlans)
    {
        ArgumentNullException.ThrowIfNull(rowShapePlans);
        var plans = rowShapePlans
            .Where(static plan => plan.FutureDroppableColumns.Length > 0)
            .Select(CreatePlan)
            .ToArray();

        return new RowWidthPruningPlanningResult(plans, plans.Select(CreateDecision).ToArray());
    }

    private static RowWidthPruningPlan CreatePlan(BoundaryRowShapePlan plan)
    {
        if (CanApplyOrderedPruning(plan))
            return CreateAppliedPlan(plan);

        if (CanApplyHashBuildPruning(plan))
            return CreateAppliedHashBuildPlan(plan);

        if (CanApplyHashProbePruning(plan))
            return CreateAppliedHashProbePlan(plan);

        if (CanApplyCteMaterializationPruning(plan))
            return CreateAppliedCteMaterializationPlan(plan);

        if (CanApplyWindowPruning(plan))
            return CreateAppliedWindowPlan(plan);

        if (CanApplyAggregatePruning(plan))
            return CreateAppliedAggregatePlan(plan);

        if (CanApplyDistinctPruning(plan))
            return CreateAppliedDistinctPlan(plan);

        if (CanApplySetOperationPruning(plan))
            return CreateAppliedSetOperationPlan(plan);

        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.DiagnosticOnly,
            plan.FutureDroppableColumns,
            [],
            plan.Confidence,
            $"{plan.Kind} row-width pruning was recorded for analysis; no physical pruning was applied for this boundary.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static bool CanApplyOrderedPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind is BoundaryRowShapeKind.Sort or BoundaryRowShapeKind.TopN or BoundaryRowShapeKind.TopOffset &&
               plan.BoundaryOnlyColumns.Length > 0;
    }

    private static bool CanApplyHashBuildPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.HashJoinBuild &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplyHashProbePruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.HashJoinProbe &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplyWindowPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.Window &&
               plan.NeededAfterBoundaryColumns.Length > 0 &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplySetOperationPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.SetOperation &&
               plan.NeededAfterBoundaryColumns.Length > 0 &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplyCteMaterializationPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.CteMaterialization &&
               plan.NeededAfterBoundaryColumns.Length > 0 &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplyAggregatePruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.Aggregate &&
               plan.NeededAfterBoundaryColumns.Length > 0 &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static bool CanApplyDistinctPruning(BoundaryRowShapePlan plan)
    {
        return plan.Kind == BoundaryRowShapeKind.Distinct &&
               plan.NeededAfterBoundaryColumns.Length > 0 &&
               plan.FutureDroppableColumns.Length > 0;
    }

    private static RowWidthPruningPlan CreateAppliedPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.BoundaryOnlyColumns,
            PlanningConfidence.High,
            $"{plan.Kind} row-width pruning drops order-only columns immediately after the ordering boundary.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedHashBuildPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.High,
            $"{plan.Kind} row-width pruning drops build-side payload columns after hash keys are constructed.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedHashProbePlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.High,
            $"{plan.Kind} row-width pruning drops probe-side payload columns after hash probe keys are constructed.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedCteMaterializationPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.Medium,
            $"{plan.Kind} row-width pruning drops materialized columns that no CTE consumer requires.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedWindowPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.Medium,
            $"{plan.Kind} row-width pruning drops window-only columns after materialization.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedAggregatePlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.Medium,
            $"{plan.Kind} row-width pruning drops aggregate input-only columns after finalization.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedDistinctPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.Medium,
            $"{plan.Kind} row-width pruning drops post-distinct columns that are unused downstream.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static RowWidthPruningPlan CreateAppliedSetOperationPlan(BoundaryRowShapePlan plan)
    {
        return new RowWidthPruningPlan(
            plan.BoundaryId,
            plan.Kind,
            RowWidthPruningStrategy.Applied,
            plan.FutureDroppableColumns,
            plan.FutureDroppableColumns,
            PlanningConfidence.Medium,
            $"{plan.Kind} row-width pruning drops symmetric arm columns unused by set comparison or downstream consumers.")
        {
            RetainedColumns = CreateRetainedColumns(plan)
        };
    }

    private static string[] CreateRetainedColumns(BoundaryRowShapePlan plan)
    {
        return plan.NeededAfterBoundaryColumns
            .Where(column => !ContainsColumn(plan.FutureDroppableColumns, column))
            .OrderBy(static column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PlanningDecision CreateDecision(RowWidthPruningPlan plan)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.RowWidthPruning,
            "RowWidthPruningPlan",
            plan.BoundaryId,
            plan.Strategy.ToString(),
            plan.Confidence,
            plan.Reason);
    }

    private static bool ContainsColumn(IReadOnlyList<string> columns, string column)
    {
        return columns.Any(candidate =>
            string.Equals(candidate, column, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(GetColumnName(candidate), GetColumnName(column), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetColumnName(string column)
    {
        var separatorIndex = column.LastIndexOf('.');
        return separatorIndex < 0 ? column : column[(separatorIndex + 1)..];
    }
}
