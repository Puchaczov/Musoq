using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarAppendIndexSpec(
    ExecutionVariable Index,
    ExecutionExpression Key,
    ExecutionCteSidecarIndexKind Kind,
    Type KeyType,
    HashPayloadShape? PayloadShape,
    IReadOnlyList<ExecutionRowValue> PayloadValues);
