namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAssign(
    ExecutionVariable Variable,
    ExecutionExpression Value) : ExecutionNode;
