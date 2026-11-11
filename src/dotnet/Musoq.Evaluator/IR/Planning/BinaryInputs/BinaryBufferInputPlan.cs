namespace Musoq.Evaluator.IR.Planning;

internal enum BinaryBufferInputKind
{
    Snapshot,
}

internal enum BinaryBufferEmptyResult
{
    EmptyArray,
    Null,
}

internal sealed record BinaryBufferInputPlan(
    BinaryBufferInputKind Kind,
    int? MaximumReadBytes,
    BinaryBufferEmptyResult EmptyResult,
    bool HasRange,
    string Reason
);
