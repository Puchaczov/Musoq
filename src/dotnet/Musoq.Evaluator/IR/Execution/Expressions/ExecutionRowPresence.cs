namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowPresence(
    string Alias,
    bool IsPresent,
    ExecutionExpression PresenceSource) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(bool)));
