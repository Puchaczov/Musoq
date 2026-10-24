namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTakeOrderRecordSelection(PagingCount Count) : ExecutionOrderRecordSelection
{
    public ExecutionTakeOrderRecordSelection(int count) : this((PagingCount)count)
    {
    }
}
