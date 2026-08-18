using System.Collections.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

public enum SourceTransferMode
{
    DeclaredRows,
    QueryScopedRows
}

public enum SourceQueryRowCarrier
{
    ReadonlyStruct,
    SealedClass
}

public enum SourceQueryRowLifetime
{
    ScanLocal,
    EscapesScan
}

public sealed record SourceTransferStrategyPlan(
    string SourceContextId,
    SourceTransferMode Mode,
    SourceQueryRowCarrier? Carrier,
    QueryRowShape? Shape,
    string Reason)
{
    public SourceQueryRowLifetime? Lifetime { get; init; }

    public static SourceTransferStrategyPlan Legacy(string sourceContextId, string reason)
    {
        return new SourceTransferStrategyPlan(
            sourceContextId,
            SourceTransferMode.DeclaredRows,
            null,
            null,
            reason);
    }
}

internal sealed record SourceTransferPlanningResult(
    IReadOnlyDictionary<string, SourceTransferStrategyPlan> PlansBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
