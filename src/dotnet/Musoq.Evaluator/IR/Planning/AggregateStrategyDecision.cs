namespace Musoq.Evaluator.IR.Planning;

internal sealed record AggregateStrategyDecision(
    AggregateStrategyKind Kind,
    string Reason,
    bool InitializeConstantGroup = false
);
