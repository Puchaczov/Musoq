using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning.SourcePlanning;

internal sealed record SourcePlanningResult(
    IReadOnlyDictionary<string, SourcePlanRequest> RequestsBySourceId,
    IReadOnlyDictionary<string, SourcePlanResult> ResultsBySourceId,
    IReadOnlyDictionary<string, SourceDescriptor> DescriptorsBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
