using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAsOfTieBreak(
    ExecutionExpression Key,
    bool Descending,
    NullOrdering NullOrdering);
