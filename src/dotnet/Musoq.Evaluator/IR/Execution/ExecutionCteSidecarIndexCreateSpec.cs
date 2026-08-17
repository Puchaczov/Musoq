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
        : this(index, kind, ExecutionClrBindingFactory.FromClr(keyType), capacityHint, ExecutionClrBindingFactory.FromOptionalClr(rowType), generatedRowTypeName)
    {
    }
}
