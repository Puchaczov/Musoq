namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnUsage(
    string SourceContextId,
    string Alias,
    string ColumnName,
    RequiredColumnUsageReason UsageReason,
    PlanningConfidence Confidence);
