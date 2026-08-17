using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMaterializeRecordListToTable : ExecutionNode
{
    public ExecutionMaterializeRecordListToTable(
        ExecutionVariable source,
        ExecutionVariable target,
        GeneratedRecordShape recordShape,
        GeneratedRowShape rowShape,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<int> renumberFieldIndexes,
        ExecutionCapacityHint? capacityHint = null,
        ExecutionAppendMode appendMode = ExecutionAppendMode.Checked)
    {
        Source = source;
        Target = target;
        RecordShape = recordShape;
        RowShape = rowShape;
        FieldIndexes = ExecutionIrCollections.Freeze(fieldIndexes);
        RenumberFieldIndexes = ExecutionIrCollections.Freeze(renumberFieldIndexes);
        CapacityHint = capacityHint;
        AppendMode = appendMode;
    }

    public ExecutionVariable Source { get; init; }
    public ExecutionVariable Target { get; init; }
    public GeneratedRecordShape RecordShape { get; init; }
    public GeneratedRowShape RowShape { get; init; }
    public IReadOnlyList<int> FieldIndexes { get; init; }
    public IReadOnlyList<int> RenumberFieldIndexes { get; init; }
    public ExecutionCapacityHint? CapacityHint { get; init; }
    public ExecutionAppendMode AppendMode { get; init; }
}
