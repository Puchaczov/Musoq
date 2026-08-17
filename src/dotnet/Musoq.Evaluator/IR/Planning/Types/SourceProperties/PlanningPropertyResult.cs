using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningPropertyResult(
    PlanningFacts Facts,
    IReadOnlyList<PlanningDecision> Decisions)
{
    public PlanProperties Properties => Facts.ToPlanProperties();
}
