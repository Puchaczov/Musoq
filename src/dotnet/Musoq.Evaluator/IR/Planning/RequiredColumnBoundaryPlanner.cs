using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class RequiredColumnBoundaryPlanner
{
    public static RequiredColumnBoundaryPlanningResult Plan(
        PhysicalNode physicalPlan,
        IReadOnlyList<BoundaryRowShapePlan> rowShapePlans)
    {
        ArgumentNullException.ThrowIfNull(physicalPlan);
        ArgumentNullException.ThrowIfNull(rowShapePlans);

        var plans = new List<RequiredColumnBoundaryPlan>();
        plans.AddRange(rowShapePlans.SelectMany(CreateBoundaryPlans));

        var joinCollector = new JoinEdgeCollector();
        joinCollector.Visit(physicalPlan, SchemaColumns(physicalPlan.OutputSchema));
        plans.AddRange(joinCollector.Plans);

        return new RequiredColumnBoundaryPlanningResult(
            plans,
            plans.Select(CreateDecision).ToArray());
    }

    private static IEnumerable<RequiredColumnBoundaryPlan> CreateBoundaryPlans(BoundaryRowShapePlan plan)
    {
        if (!TryMapBoundaryKind(plan.Kind, out var kind))
            yield break;

        var required = OrderColumns(plan.SemanticColumns.Length > 0
            ? plan.SemanticColumns
            : plan.NeededAfterBoundaryColumns);
        var retained = OrderColumns(plan.RetainedExecutionColumns.Length > 0
            ? plan.RetainedExecutionColumns
            : required);
        var blocked = OrderColumns(plan.BlockedColumns);

        yield return new RequiredColumnBoundaryPlan(
            plan.BoundaryId,
            kind,
            required,
            retained,
            blocked,
            CreateMappings(retained),
            plan.Confidence,
            $"{plan.Kind} required-column boundary facts were mapped for runtime-v2 projection planning.");
    }

    private static bool TryMapBoundaryKind(BoundaryRowShapeKind source, out RequiredColumnBoundaryKind target)
    {
        switch (source)
        {
            case BoundaryRowShapeKind.Aggregate:
                target = RequiredColumnBoundaryKind.Aggregate;
                return true;
            case BoundaryRowShapeKind.Window:
                target = RequiredColumnBoundaryKind.Window;
                return true;
            case BoundaryRowShapeKind.SetOperation:
                target = RequiredColumnBoundaryKind.SetOperation;
                return true;
            case BoundaryRowShapeKind.HashJoinBuild:
                target = RequiredColumnBoundaryKind.HashJoinBuild;
                return true;
            case BoundaryRowShapeKind.CteMaterialization:
                target = RequiredColumnBoundaryKind.CteMaterialization;
                return true;
            default:
                target = default;
                return false;
        }
    }

    private static PlanningDecision CreateDecision(RequiredColumnBoundaryPlan plan)
    {
        var outcome = plan.BlockedColumns.Length > 0
            ? "MappedWithBlockedColumns"
            : plan.RequiredColumns.Length == 0
                ? "NoRequiredColumns"
                : "Mapped";

        return new PlanningDecision(
            PlanningDecisionCategory.RequiredColumns,
            "RequiredColumnBoundary",
            plan.BoundaryId,
            outcome,
            plan.Confidence,
            plan.Reason);
    }
}
