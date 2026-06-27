namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionHoistCandidateLet(
    ExecutionVariable Variable,
    ExecutionExpression Value,
    ExecutionHoistKind Kind,
    ExecutionHoistScope Scope,
    string Signature) : ExecutionNode;
