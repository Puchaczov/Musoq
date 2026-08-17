namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionNullContextArray(int Count)
    : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object[])));
