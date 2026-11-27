namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed class ParallelIntermediateTableSlots
{
    private int _next;

    public int Reserve(int firstAvailable, int count)
    {
        var first = Math.Max(firstAvailable, _next);
        _next = checked(first + count);
        return first;
    }
}
