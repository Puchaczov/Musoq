namespace Musoq.Evaluator.IR.Planning;

internal sealed record BoundaryRowShapePlan(
    string BoundaryId,
    BoundaryRowShapeKind Kind,
    string[] InputColumns,
    string[] NeededAfterBoundaryColumns,
    string[] BoundaryOnlyColumns,
    string[] FutureDroppableColumns,
    PlanningConfidence Confidence,
    string Reason)
{
    public string[] SemanticColumns { get; init; } = [];

    public string[] RetainedExecutionColumns { get; init; } = [];

    public string[] CandidateColumns { get; init; } = [];

    public string[] BlockedColumns { get; init; } = [];
}
