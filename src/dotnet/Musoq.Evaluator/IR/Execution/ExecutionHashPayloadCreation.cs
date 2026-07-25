using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateHashPayload : ExecutionNode
{
    private IReadOnlyList<ExecutionRowValue> _values = [];

    public ExecutionCreateHashPayload(
        ExecutionVariable payload,
        HashPayloadShape payloadShape,
        IReadOnlyList<ExecutionRowValue> values)
    {
        Payload = payload;
        PayloadShape = payloadShape;
        Values = ExecutionIrCollections.Freeze(values);
    }

    public ExecutionVariable Payload { get; init; }

    public HashPayloadShape PayloadShape { get; init; }

    public IReadOnlyList<ExecutionRowValue> Values
    {
        get => _values;
        init => _values = ExecutionIrCollections.Freeze(value);
    }
}
