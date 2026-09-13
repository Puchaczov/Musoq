namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionDynamicLikeMatch : ExecutionExpression
{
    public ExecutionDynamicLikeMatch(
        ExecutionExpression input,
        ExecutionExpression pattern,
        ExecutionExpression cacheSlot,
        ExecutionStringMatchComparison comparison,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        CacheSlot = cacheSlot ?? throw new ArgumentNullException(nameof(cacheSlot));
        if (comparison != ExecutionStringMatchComparison.LikeIgnoreCase)
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown dynamic LIKE comparison.");

        Comparison = comparison;
    }

    public ExecutionExpression Input { get; init; }

    public ExecutionExpression Pattern { get; init; }

    public ExecutionExpression CacheSlot { get; init; }

    public ExecutionStringMatchComparison Comparison { get; }
}
