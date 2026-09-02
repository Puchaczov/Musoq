namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowOrderKey(
    ExecutionExpression Expression,
    bool Descending, Bindings.NullOrdering NullOrdering = Bindings.NullOrdering.Default);
