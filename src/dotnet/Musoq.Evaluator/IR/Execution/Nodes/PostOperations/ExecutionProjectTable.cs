using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionProjectTable : ExecutionNode
{
    public ExecutionProjectTable(
        ExecutionVariable source,
        ExecutionVariable target,
        GeneratedRowShape rowShape,
        IReadOnlyList<int> fieldIndexes,
        ExecutionCapacityHint? capacityHint = null,
        ExecutionAppendMode appendMode = ExecutionAppendMode.Checked)
    {
        Source = source;
        Target = target;
        RowShape = rowShape;
        FieldIndexes = ExecutionIrCollections.Freeze(fieldIndexes);
        CapacityHint = capacityHint;
        AppendMode = appendMode;
    }

    public ExecutionVariable Source { get; init; }
    public ExecutionVariable Target { get; init; }
    public GeneratedRowShape RowShape { get; init; }
    public IReadOnlyList<int> FieldIndexes { get; init; }
    public ExecutionCapacityHint? CapacityHint { get; init; }
    public ExecutionAppendMode AppendMode { get; init; }
}
