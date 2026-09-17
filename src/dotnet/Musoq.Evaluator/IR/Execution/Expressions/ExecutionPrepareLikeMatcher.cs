namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPrepareLikeMatcher : ExecutionExpression
{
    public ExecutionPrepareLikeMatcher(
        ExecutionExpression pattern,
        ExecutionStringMatchComparison comparison,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        if (comparison != ExecutionStringMatchComparison.LikeIgnoreCase)
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown LIKE matcher comparison.");

        Comparison = comparison;
    }

    public ExecutionExpression Pattern { get; init; }

    public ExecutionStringMatchComparison Comparison { get; }
}
