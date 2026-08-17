namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAppendExistingRow(
    ExecutionVariable Table,
    ExecutionVariable Row,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked) : ExecutionNode;
