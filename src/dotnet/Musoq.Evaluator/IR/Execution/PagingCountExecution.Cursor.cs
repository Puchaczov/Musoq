using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class PagingCountExecution
{
    internal static ExecutionExpression? ReadAt(ExecutionNode node, int index) => node switch
    {
        ExecutionSkipTable skip => ReadAt(skip.Count, null, index),
        ExecutionTakeTable take => ReadAt(take.Count, null, index),
        ExecutionSliceTable slice => ReadAt(slice.SkipCount, slice.TakeCount, index),
        ExecutionTopNTable top => ReadAt(top.Count, null, index),
        ExecutionTopOffsetTable top => ReadAt(top.SkipCount, top.TakeCount, index),
        ExecutionCreateBoundedRecordList bounded => ReadAt(bounded.Selection, index),
        ExecutionOrderRecordList order => ReadAt(order.Selection, index),
        _ => null
    };

    private static ExecutionExpression? ReadAt(ExecutionOrderRecordSelection selection, int index) => selection switch
    {
        ExecutionTakeOrderRecordSelection take => ReadAt(take.Count, null, index),
        ExecutionSkipTakeOrderRecordSelection slice => ReadAt(slice.SkipCount, slice.TakeCount, index),
        _ => null
    };

    private static ExecutionExpression? ReadAt(PagingCount first, PagingCount? second, int index)
    {
        if (!first.IsConstant)
        {
            if (index == 0)
                return Read(first);
            index--;
        }
        return index == 0 && second is { IsConstant: false } ? Read(second.Value) : null;
    }
}
