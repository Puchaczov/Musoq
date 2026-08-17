namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionWindowFrameBoundKind
{
    UnboundedPreceding,
    UnboundedFollowing,
    CurrentRow,
    OffsetPreceding,
    OffsetFollowing
}
