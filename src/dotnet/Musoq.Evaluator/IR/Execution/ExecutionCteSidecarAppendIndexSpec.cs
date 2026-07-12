using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarAppendIndexSpec(
    ExecutionVariable Index,
    ExecutionExpression Key,
    ExecutionCteSidecarIndexKind Kind,
    ExecutionTypeRef KeyType,
    HashPayloadShape? PayloadShape,
    IReadOnlyList<ExecutionRowValue> PayloadValues)
{
    internal ExecutionCteSidecarAppendIndexSpec(
        ExecutionVariable index,
        ExecutionExpression key,
        ExecutionCteSidecarIndexKind kind,
        Type keyType,
        HashPayloadShape? payloadShape,
        IReadOnlyList<ExecutionRowValue> payloadValues)
        : this(index, key, kind, ExecutionTypeRef.FromClr(keyType), payloadShape, payloadValues)
    {
    }
}
