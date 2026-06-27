using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarIndexStoreCandidate(
    ExecutionVariable Index,
    int IndexSlot,
    ExecutionCteSidecarIndexKind Kind,
    Type KeyType,
    Type? RowType = null,
    string? GeneratedRowTypeName = null) : ExecutionNode;
