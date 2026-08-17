namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionStoreTable(
    ExecutionVariable Table,
    int TableIndex) : ExecutionNode;
