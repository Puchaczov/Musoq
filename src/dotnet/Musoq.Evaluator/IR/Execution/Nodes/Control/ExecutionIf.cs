namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIf(
    ExecutionExpression Condition,
    ExecutionBlock Body) : ExecutionNode;
