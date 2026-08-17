namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateTable(
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode;
