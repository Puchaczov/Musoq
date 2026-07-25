using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateBoundedRecordList : ExecutionNode
{
    public ExecutionCreateBoundedRecordList(
        ExecutionVariable list,
        GeneratedRecordShape recordShape,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionOrderRecordSelection selection)
    {
        List = list;
        RecordShape = recordShape;
        Keys = ExecutionIrCollections.Freeze(keys);
        Selection = selection;
    }

    public ExecutionVariable List { get; init; }
    public GeneratedRecordShape RecordShape { get; init; }
    public IReadOnlyList<ExecutionOrderField> Keys { get; init; }
    public ExecutionOrderRecordSelection Selection { get; init; }
}
