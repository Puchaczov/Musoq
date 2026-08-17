namespace Musoq.Evaluator.IR.Planning;

internal sealed record RequiredColumnUsageKey(
    string SourceContextId,
    string Alias,
    string ColumnName,
    RequiredColumnUsageReason Reason);
