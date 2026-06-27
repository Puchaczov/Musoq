using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

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
