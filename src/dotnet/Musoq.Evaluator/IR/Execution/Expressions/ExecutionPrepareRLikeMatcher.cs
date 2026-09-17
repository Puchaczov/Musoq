namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPrepareRLikeMatcher : ExecutionExpression
{
    public ExecutionPrepareRLikeMatcher(
        ExecutionExpression pattern,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    public ExecutionExpression Pattern { get; init; }
}
