namespace Musoq.Evaluator.IR.Planning;

internal sealed record RowWidthPruningPlan(
    string BoundaryId,
    BoundaryRowShapeKind Kind,
    RowWidthPruningStrategy Strategy,
    string[] CandidateColumns,
    string[] PrunedColumns,
    PlanningConfidence Confidence,
    string Reason)
{
    public string[] RetainedColumns { get; init; } = [];
}
