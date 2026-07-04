namespace Musoq.Evaluator.IR.Execution;

internal sealed record CteSidecarStorageDecision(
    bool StoreRows,
    bool KeepPayloadRows);
