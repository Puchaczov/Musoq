using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTopNTable : ExecutionNode
{
    public ExecutionTopNTable(
        ExecutionVariable source,
        ExecutionVariable target,
        IReadOnlyList<ExecutionOrderField> keys,
        int count,
        IReadOnlyList<int> renumberFieldIndexes,
        ExecutionCapacityHint? capacityHint = null,
        ExecutionAppendMode appendMode = ExecutionAppendMode.Checked,
        ExecutionColumnMetadata? columnMetadata = null)
    {
        Source = source;
        Target = target;
        Keys = ExecutionIrCollections.Freeze(keys);
        Count = count;
        RenumberFieldIndexes = ExecutionIrCollections.Freeze(renumberFieldIndexes);
        CapacityHint = capacityHint;
        AppendMode = appendMode;
        ColumnMetadata = columnMetadata;
    }

    public ExecutionVariable Source { get; init; }
    public ExecutionVariable Target { get; init; }
    public IReadOnlyList<ExecutionOrderField> Keys { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<int> RenumberFieldIndexes { get; init; }
    public ExecutionCapacityHint? CapacityHint { get; init; }
    public ExecutionAppendMode AppendMode { get; init; }
    public ExecutionColumnMetadata? ColumnMetadata { get; init; }
}
