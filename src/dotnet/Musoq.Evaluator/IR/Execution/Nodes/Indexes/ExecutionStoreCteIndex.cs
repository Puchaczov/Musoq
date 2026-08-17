namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoreCteIndex(
    ExecutionVariable Index,
    int IndexSlot,
    ExecutionCteSidecarIndexKind Kind,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef? RowType = null,
    string? GeneratedRowTypeName = null) : ExecutionNode
{
    internal ExecutionStoreCteIndex(
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
