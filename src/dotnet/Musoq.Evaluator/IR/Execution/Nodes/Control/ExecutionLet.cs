namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLet(
    ExecutionVariable Variable,
    ExecutionExpression Value,
    ExecutionLetCacheMode CacheMode = ExecutionLetCacheMode.Default) : ExecutionNode;
