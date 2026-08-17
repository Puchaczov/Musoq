namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionDistinctTable(
    ExecutionVariable Source,
    ExecutionVariable Target) : ExecutionNode;
