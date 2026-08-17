namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScalarRowStream(
    ExecutionVariable Variable) : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(object)));
