using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateHashPayload(
    ExecutionVariable Payload,
    HashPayloadShape PayloadShape,
    IReadOnlyList<ExecutionRowValue> Values) : ExecutionNode;
