using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAppendRow : ExecutionNode
{
    public ExecutionAppendRow(
        ExecutionVariable table,
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts,
        ExecutionAppendMode appendMode = ExecutionAppendMode.Checked,
        ExecutionContextLayout? contextLayout = null)
    {
        Table = table;
        RowShape = rowShape;
        Values = ExecutionIrCollections.Freeze(values);
        Contexts = ExecutionIrCollections.Freeze(contexts);
        AppendMode = appendMode;
        ContextLayout = contextLayout;
    }

    public ExecutionVariable Table { get; init; }
    public GeneratedRowShape RowShape { get; init; }
    public IReadOnlyList<ExecutionRowValue> Values { get; init; }
    public IReadOnlyList<ExecutionExpression> Contexts { get; init; }
    public ExecutionAppendMode AppendMode { get; init; }
    public ExecutionContextLayout? ContextLayout { get; init; }

    public ExecutionAppendRow(
        ExecutionVariable table,
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values)
        : this(table, rowShape, values, [])
    {
    }

    public ExecutionAppendRow(
        ExecutionVariable table,
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionRowValue> values,
        ExecutionAppendMode appendMode)
        : this(table, rowShape, values, [], appendMode)
    {
    }
}
