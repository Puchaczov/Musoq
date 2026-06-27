using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMaterializeFilteredList(
    ExecutionExpression Source,
    ExecutionVariable Buffer,
    ExecutionVariable Item,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionExpression Predicate,
    GeneratedRowShape? GeneratedRowShape = null) : ExecutionNode;
