using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal sealed record SourcePlanningResult(
    IReadOnlyDictionary<string, SourcePlanRequest> RequestsBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> ResultsBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
