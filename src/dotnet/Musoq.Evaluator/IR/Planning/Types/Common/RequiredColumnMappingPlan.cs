namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnMappingPlan(
    string SourceContextId,
    string Alias,
    string[] RequiredColumns,
    string[] RetainedColumns,
    string[] BlockedColumns,
    string[] OriginOutputMappings,
    PlanningConfidence Confidence,
    string Reason);
