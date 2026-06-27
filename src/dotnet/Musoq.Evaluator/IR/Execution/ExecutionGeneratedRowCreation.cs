using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateGeneratedRow(
    ExecutionVariable Row,
    GeneratedRowShape RowShape,
    IReadOnlyList<ExecutionRowValue> Values,
    IReadOnlyList<ExecutionExpression> Contexts,
    ExecutionContextLayout? ContextLayout = null) : ExecutionNode;
