namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowFrameBound(
    ExecutionWindowFrameBoundKind Kind,
    int Offset);
