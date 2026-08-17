using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteSidecarIndexPlanningResult(
    CteSidecarIndexPlan Plan,
    IReadOnlyList<PlanningDecision> Decisions,
    int NextIndexSlot)
{
    public static CteSidecarIndexPlanningResult Empty(int nextIndexSlot)
    {
        return new CteSidecarIndexPlanningResult(CteSidecarIndexPlan.Empty, [], nextIndexSlot);
    }
}
