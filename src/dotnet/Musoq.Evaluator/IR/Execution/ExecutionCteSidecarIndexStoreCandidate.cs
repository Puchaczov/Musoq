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
        : this(index, indexSlot, kind, ExecutionClrBindingFactory.FromClr(keyType), ExecutionClrBindingFactory.FromOptionalClr(rowType), generatedRowTypeName)
    {
    }
}
