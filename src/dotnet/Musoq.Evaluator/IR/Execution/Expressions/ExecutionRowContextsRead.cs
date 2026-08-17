namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionRowContextsRead(ExecutionVariable Row)
    : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object[])));
