namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSkipTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    int Count,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode;
