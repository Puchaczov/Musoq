namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodTargetScope(
    ExecutionMethodTargetScopeKind Kind,
    string Name);
