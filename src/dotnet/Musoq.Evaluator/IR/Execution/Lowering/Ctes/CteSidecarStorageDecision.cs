namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record CteSidecarStorageDecision(
    bool StoreRows,
    bool KeepPayloadRows);
