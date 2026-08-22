namespace Musoq.Evaluator.IR.Execution;

/// <summary>
///     An observable, once-only entry marker for a query clause or query scope.
/// </summary>
public sealed record ExecutionPhaseBoundary(
    QueryPhase Phase,
    string QueryIdSuffix = "") : ExecutionNode
{
    public ExecutionPhaseBoundary(QueryPhase phase)
        : this(phase, string.Empty)
    {
    }

}
