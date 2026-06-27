namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMethodTargetDeclarationCandidate(
    ExecutionVariable Target,
    ExecutionMethodTargetScope? Scope = null) : ExecutionNode;
