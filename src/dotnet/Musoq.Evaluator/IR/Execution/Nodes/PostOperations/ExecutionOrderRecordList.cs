using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionOrderRecordList : ExecutionNode
{
    public ExecutionOrderRecordList(
        ExecutionVariable source,
        GeneratedRecordShape recordShape,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionOrderRecordSelection selection)
    {
        Source = source;
        RecordShape = recordShape;
        Keys = ExecutionIrCollections.Freeze(keys);
        Selection = selection;
    }

    public ExecutionVariable Source { get; init; }
    public GeneratedRecordShape RecordShape { get; init; }
    public IReadOnlyList<ExecutionOrderField> Keys { get; init; }
    public ExecutionOrderRecordSelection Selection { get; init; }
}
