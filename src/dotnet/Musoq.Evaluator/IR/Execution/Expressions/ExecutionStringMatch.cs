namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionStringMatchKind
{
    Exact,
    Prefix,
    Suffix,
    Contains
}

public enum ExecutionStringMatchComparison
{
    LikeIgnoreCase,
    Ordinal
}

public sealed record ExecutionStringMatch : ExecutionExpression
{
    public ExecutionStringMatch(
        ExecutionExpression input,
        string originalPattern,
        string needle,
        ExecutionStringMatchKind kind,
        ExecutionStringMatchComparison comparison,
        ExecutionTypeRef returnType)
        : base(returnType)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        OriginalPattern = originalPattern ?? throw new ArgumentNullException(nameof(originalPattern));
        Needle = needle ?? throw new ArgumentNullException(nameof(needle));
        if (!Enum.IsDefined(comparison))
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown string-match comparison.");

        Kind = kind;
        Comparison = comparison;
    }

    public ExecutionExpression Input { get; init; }

    public string OriginalPattern { get; }

    public string Needle { get; }

    public ExecutionStringMatchKind Kind { get; }

    public ExecutionStringMatchComparison Comparison { get; }
}
