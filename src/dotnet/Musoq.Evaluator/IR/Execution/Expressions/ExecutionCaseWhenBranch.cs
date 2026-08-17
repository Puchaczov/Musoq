namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCaseWhenBranch(
    ExecutionExpression Condition,
    ExecutionExpression Result);
