using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

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
