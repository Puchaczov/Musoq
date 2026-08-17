namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowKeyArray(
    ExecutionVariable Variable,
    bool ShouldExtract,
    ExecutionWindowKeyShape? Shape = null,
    bool ShouldMaterialize = true);
