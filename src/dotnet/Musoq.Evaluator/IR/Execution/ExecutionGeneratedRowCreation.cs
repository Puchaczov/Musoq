using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateGeneratedRow : ExecutionNode
{
    private IReadOnlyList<ExecutionRowValue> _values = [];
    private IReadOnlyList<ExecutionExpression> _contexts = [];

    public ExecutionCreateGeneratedRow(
        ExecutionVariable row,
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout = null)
    {
        Row = row;
        RowShape = rowShape;
        Values = ExecutionIrCollections.Freeze(values);
        Contexts = ExecutionIrCollections.Freeze(contexts);
        ContextLayout = contextLayout;
    }

    public ExecutionVariable Row { get; init; }

    public GeneratedRowShape RowShape { get; init; }

    public IReadOnlyList<ExecutionRowValue> Values
    {
        get => _values;
        init => _values = ExecutionIrCollections.Freeze(value);
    }

    public IReadOnlyList<ExecutionExpression> Contexts
    {
        get => _contexts;
        init => _contexts = ExecutionIrCollections.Freeze(value);
    }

    public ExecutionContextLayout? ContextLayout { get; init; }
}
