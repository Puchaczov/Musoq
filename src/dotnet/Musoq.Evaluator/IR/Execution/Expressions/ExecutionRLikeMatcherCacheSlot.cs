namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRLikeMatcherCacheSlot(
    bool WorkerLocal,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    public ExecutionRLikeMatcherCacheSlot(ExecutionTypeRef returnType)
        : this(false, returnType)
    {
    }
}
