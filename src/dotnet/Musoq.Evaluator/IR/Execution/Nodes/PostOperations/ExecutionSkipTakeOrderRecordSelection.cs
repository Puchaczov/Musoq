namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSkipTakeOrderRecordSelection(PagingCount SkipCount, PagingCount TakeCount) : ExecutionOrderRecordSelection
{
    public ExecutionSkipTakeOrderRecordSelection(int skipCount, int takeCount) : this((PagingCount)skipCount, (PagingCount)takeCount)
    {
    }
}
