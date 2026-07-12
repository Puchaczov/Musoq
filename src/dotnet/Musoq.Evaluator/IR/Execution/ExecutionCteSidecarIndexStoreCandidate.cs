using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ExecutionCteSidecarIndexStoreCandidate(
    ExecutionVariable Index,
    int IndexSlot,
    ExecutionCteSidecarIndexKind Kind,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef? RowType = null,
    string? GeneratedRowTypeName = null) : ExecutionNode
{
    internal ExecutionCteSidecarIndexStoreCandidate(
        ExecutionVariable index,
        int indexSlot,
        ExecutionCteSidecarIndexKind kind,
        Type keyType,
        Type? rowType = null,
        string? generatedRowTypeName = null)
        : this(index, indexSlot, kind, ExecutionTypeRef.FromClr(keyType), ExecutionTypeRef.FromOptionalClr(rowType), generatedRowTypeName)
    {
    }
}
