namespace Musoq.Evaluator.IR.Execution;

public abstract record ExecutionSourceLoop(
    ExecutionVariable Item,
    ExecutionExpression Source,
    ExecutionBlock Body) : ExecutionNode;
