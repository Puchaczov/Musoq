namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSourceScan(
    ExecutionVariable Source,
    ExecutionVariable Rows,
    ExecutionSourceBinding Binding) : ExecutionNode;
