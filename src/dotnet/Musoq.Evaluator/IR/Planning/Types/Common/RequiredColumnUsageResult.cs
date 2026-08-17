using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnUsageResult(
    IReadOnlyDictionary<string, IReadOnlySet<string>> RequiredColumnsByAlias,
    IReadOnlyDictionary<string, RequiredColumnUsage[]> UsagesBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
