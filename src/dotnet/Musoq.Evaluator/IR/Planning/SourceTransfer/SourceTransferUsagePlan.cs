using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal enum SourceRowRequirement
{
    ColumnValuesOnly,
    DeclaredEntity
}

internal enum SourceRowLifetime
{
    ScanLocal,
    EscapesScan
}

internal sealed record SourceTransferUsagePlan(
    string SourceContextId,
    SourceRowRequirement RowRequirement,
    SourceRowLifetime Lifetime,
    string RowRequirementReason,
    string LifetimeReason);

internal sealed record SourceTransferUsagePlanningResult(
    IReadOnlyDictionary<string, SourceTransferUsagePlan> PlansBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
