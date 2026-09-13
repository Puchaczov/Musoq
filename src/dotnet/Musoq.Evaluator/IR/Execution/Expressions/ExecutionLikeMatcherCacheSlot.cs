namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionLikeMatcherCacheSlot(
    bool WorkerLocal,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    public ExecutionLikeMatcherCacheSlot(ExecutionTypeRef returnType)
        : this(false, returnType)
    {
    }
}
