using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAppendRecord : ExecutionNode
{
    public ExecutionAppendRecord(
        ExecutionVariable list,
        GeneratedRecordShape recordShape,
        IReadOnlyList<ExecutionRowValue> values)
    {
        List = list;
        RecordShape = recordShape;
        Values = ExecutionIrCollections.Freeze(values);
    }

    public ExecutionVariable List { get; init; }
    public GeneratedRecordShape RecordShape { get; init; }
    public IReadOnlyList<ExecutionRowValue> Values { get; init; }
}
