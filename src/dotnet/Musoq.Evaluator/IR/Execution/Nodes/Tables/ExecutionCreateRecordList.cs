namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateRecordList(
    ExecutionVariable List,
    GeneratedRecordShape RecordShape,
    ExecutionCapacityHint? CapacityHint = null) : ExecutionNode;
