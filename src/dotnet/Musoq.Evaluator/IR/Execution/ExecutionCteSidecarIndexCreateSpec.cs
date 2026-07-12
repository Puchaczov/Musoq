using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarIndexCreateSpec(
    ExecutionVariable Index,
    ExecutionCteSidecarIndexKind Kind,
    ExecutionTypeRef KeyType,
    ExecutionCapacityHint? CapacityHint,
    ExecutionTypeRef? RowType = null,
    string? GeneratedRowTypeName = null)
{
    internal ExecutionCteSidecarIndexCreateSpec(
        ExecutionVariable index,
        ExecutionCteSidecarIndexKind kind,
        Type keyType,
        ExecutionCapacityHint? capacityHint,
        Type? rowType = null,
        string? generatedRowTypeName = null)
        : this(index, kind, ExecutionTypeRef.FromClr(keyType), capacityHint, ExecutionTypeRef.FromOptionalClr(rowType), generatedRowTypeName)
    {
    }
}
