using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAppendRow(
    ExecutionVariable Table,
    GeneratedRowShape RowShape,
    IReadOnlyList<ExecutionRowValue> Values,
    IReadOnlyList<ExecutionExpression> Contexts,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionContextLayout? ContextLayout = null) : ExecutionNode
{
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
