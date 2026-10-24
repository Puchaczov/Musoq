namespace Musoq.Evaluator.IR.Execution.Facts;

internal struct ExecutionLocalExpressionCursor(ExecutionNode owner)
{
    private int _index;
    private int _row;
    private int _column;
    internal ExecutionExpression Current { get; private set; } = null!;

    internal bool MoveNext()
    {
        if (owner is ExecutionCreateValuesRows values)
            return MoveNextValue(values);
        var expression = ExecutionNodeFacts.GetLocalExpressionAt(owner, _index++);
        if (expression is null)
            return false;
        Current = expression;
        return true;
    }

    private bool MoveNextValue(ExecutionCreateValuesRows values)
    {
        while (_row < values.Values.Count)
        {
            if (_column < values.Values[_row].Count)
            {
                Current = values.Values[_row][_column++].Value;
                return true;
            }
            _row++;
            _column = 0;
        }
        return false;
    }
}
