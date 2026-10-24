namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTakeTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    PagingCount Count,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode
{
    public ExecutionTakeTable(ExecutionVariable Source, ExecutionVariable Target, int Count,
        ExecutionCapacityHint? CapacityHint = null, ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
        ExecutionColumnMetadata? ColumnMetadata = null)
        : this(Source, Target, (PagingCount)Count, CapacityHint, AppendMode, ColumnMetadata)
    {
    }
}
