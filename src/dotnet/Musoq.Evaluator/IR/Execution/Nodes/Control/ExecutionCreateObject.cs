namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateObject(
    ExecutionVariable Target) : ExecutionNode;
