using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Physical.SourcePlanning;

internal sealed record SourcePlanPhysicalRewriteResult(
    PhysicalNode PhysicalPlan,
    IReadOnlyDictionary<string, SourcePlanResult> SourcePlanResultsBySourceId);
