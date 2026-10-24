namespace Musoq.Evaluator.IR.Execution.Facts;

internal struct ExecutionExpressionChildCursor(ExecutionExpression owner)
{
    private int _index;
    internal ExecutionExpression Current { get; private set; } = null!;

    internal bool MoveNext()
    {
        var child = ExecutionExpressionChildren.GetAt(owner, _index++);
        if (child is null)
            return false;
        Current = child;
        return true;
    }
}
