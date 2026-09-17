namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPreparedRLikeMatch : ExecutionExpression
{
    public ExecutionPreparedRLikeMatch(
        ExecutionExpression input,
        ExecutionExpression matcher,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
    }

    public ExecutionExpression Input { get; init; }

    public ExecutionExpression Matcher { get; init; }
}
