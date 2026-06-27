using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateValuesRows(
    ExecutionVariable Rows,
    GeneratedRowShape RowShape,
    IReadOnlyList<IReadOnlyList<ExecutionRowValue>> Values) : ExecutionNode;
