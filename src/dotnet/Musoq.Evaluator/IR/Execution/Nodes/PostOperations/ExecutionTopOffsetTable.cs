using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTopOffsetTable : ExecutionNode
{
    public ExecutionTopOffsetTable(
        ExecutionVariable source,
        ExecutionVariable target,
        IReadOnlyList<ExecutionOrderField> keys,
        int skipCount,
        int takeCount,
        IReadOnlyList<int> renumberFieldIndexes,
        ExecutionTopOffsetStrategy strategy,
        ExecutionCapacityHint? capacityHint = null,
        ExecutionAppendMode appendMode = ExecutionAppendMode.Checked,
        ExecutionColumnMetadata? columnMetadata = null)
    {
        Source = source;
        Target = target;
        Keys = ExecutionIrCollections.Freeze(keys);
        SkipCount = skipCount;
        TakeCount = takeCount;
        RenumberFieldIndexes = ExecutionIrCollections.Freeze(renumberFieldIndexes);
        Strategy = strategy;
        CapacityHint = capacityHint;
        AppendMode = appendMode;
        ColumnMetadata = columnMetadata;
    }

    public ExecutionVariable Source { get; init; }
    public ExecutionVariable Target { get; init; }
    public IReadOnlyList<ExecutionOrderField> Keys { get; init; }
    public int SkipCount { get; init; }
    public int TakeCount { get; init; }
    public IReadOnlyList<int> RenumberFieldIndexes { get; init; }
    public ExecutionTopOffsetStrategy Strategy { get; init; }
    public ExecutionCapacityHint? CapacityHint { get; init; }
    public ExecutionAppendMode AppendMode { get; init; }
    public ExecutionColumnMetadata? ColumnMetadata { get; init; }
}
