namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionInitializeAggregateState(
    ExecutionVariable Target,
    ExecutionCallableRef ResetMethod) : ExecutionNode;
