namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionDynamicRLikeMatch : ExecutionExpression
{
    public ExecutionDynamicRLikeMatch(
        ExecutionExpression input,
        ExecutionExpression pattern,
        ExecutionExpression cacheSlot,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        CacheSlot = cacheSlot ?? throw new ArgumentNullException(nameof(cacheSlot));
    }

    public ExecutionExpression Input { get; init; }

    public ExecutionExpression Pattern { get; init; }

    public ExecutionExpression CacheSlot { get; init; }
}
