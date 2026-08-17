namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionVariableRead(ExecutionVariable Variable) : ExecutionExpression(Variable.Type);
