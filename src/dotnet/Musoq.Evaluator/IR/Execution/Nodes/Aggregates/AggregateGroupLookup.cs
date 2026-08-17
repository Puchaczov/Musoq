namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupLookup(
    ExecutionVariable Variable,
    int PrefixLength);
