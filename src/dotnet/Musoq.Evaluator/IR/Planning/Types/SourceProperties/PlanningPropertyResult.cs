using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning.SourcePlanning;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningPropertyResult(
    PlanningFacts Facts,
    IReadOnlyList<PlanningDecision> Decisions,
    IReadOnlyDictionary<string, SourceComputedTransferPlan>? ComputedTransfers
)
{
    public PlanProperties Properties => Facts.ToPlanProperties();
}
