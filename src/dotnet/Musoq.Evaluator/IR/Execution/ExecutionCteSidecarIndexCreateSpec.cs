using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarIndexCreateSpec(
    ExecutionVariable Index,
    ExecutionCteSidecarIndexKind Kind,
    Type KeyType,
    ExecutionCapacityHint? CapacityHint,
    Type? RowType = null,
    string? GeneratedRowTypeName = null);
