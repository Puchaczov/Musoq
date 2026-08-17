namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAsOfEqualityKey(
    ExecutionExpression Left,
    ExecutionExpression Right);
