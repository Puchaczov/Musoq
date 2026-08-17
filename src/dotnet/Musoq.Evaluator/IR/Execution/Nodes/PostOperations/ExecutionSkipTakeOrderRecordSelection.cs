namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSkipTakeOrderRecordSelection(int SkipCount, int TakeCount) : ExecutionOrderRecordSelection;
