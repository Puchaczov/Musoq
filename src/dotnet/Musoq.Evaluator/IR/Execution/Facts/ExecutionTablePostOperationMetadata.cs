namespace Musoq.Evaluator.IR.Execution.Facts;

internal sealed record ExecutionTablePostOperationMetadata(
    ExecutionVariable Source,
    ExecutionVariable Target,
    ExecutionCapacityHint? CapacityHint,
    ExecutionAppendMode AppendMode,
    ExecutionColumnMetadata? ColumnMetadata);
