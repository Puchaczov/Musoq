namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSliceTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    PagingCount SkipCount,
    PagingCount TakeCount,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode
{
    public ExecutionSliceTable(ExecutionVariable Source, ExecutionVariable Target, int SkipCount, int TakeCount,
        ExecutionCapacityHint? CapacityHint = null, ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
        ExecutionColumnMetadata? ColumnMetadata = null)
        : this(Source, Target, (PagingCount)SkipCount, (PagingCount)TakeCount, CapacityHint, AppendMode, ColumnMetadata)
    {
    }
}
