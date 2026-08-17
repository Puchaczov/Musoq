namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnBoundaryPlan(
    string BoundaryId,
    RequiredColumnBoundaryKind Kind,
    string[] RequiredColumns,
    string[] RetainedColumns,
    string[] BlockedColumns,
    string[] OriginOutputMappings,
    PlanningConfidence Confidence,
    string Reason);
