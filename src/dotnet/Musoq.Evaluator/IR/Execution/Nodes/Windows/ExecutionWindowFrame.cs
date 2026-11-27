namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowFrame(
    ExecutionWindowFrameKind Kind,
    ExecutionWindowFrameBound Start,
    ExecutionWindowFrameBound End
)
{
    public bool HasNumericRangeOffset =>
        Kind == ExecutionWindowFrameKind.Range
        && (
            Start.Kind is ExecutionWindowFrameBoundKind.OffsetPreceding or ExecutionWindowFrameBoundKind.OffsetFollowing
            || End.Kind
                is ExecutionWindowFrameBoundKind.OffsetPreceding
                    or ExecutionWindowFrameBoundKind.OffsetFollowing
        );
}
