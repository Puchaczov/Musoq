using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateValuesRows : ExecutionNode
{
    private IReadOnlyList<IReadOnlyList<ExecutionRowValue>> _values = [];

    public ExecutionCreateValuesRows(
        ExecutionVariable rows,
        GeneratedRowShape rowShape,
        IReadOnlyList<IReadOnlyList<ExecutionRowValue>> values)
    {
        Rows = rows;
        RowShape = rowShape;
        Values = ExecutionIrCollections.FreezeNested(values);
    }

    public ExecutionVariable Rows { get; init; }

    public GeneratedRowShape RowShape { get; init; }

    public IReadOnlyList<IReadOnlyList<ExecutionRowValue>> Values
    {
        get => _values;
        init => _values = ExecutionIrCollections.FreezeNested(value);
    }
}
