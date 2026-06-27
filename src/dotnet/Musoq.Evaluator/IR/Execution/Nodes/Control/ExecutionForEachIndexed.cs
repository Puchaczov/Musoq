using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionForEachIndexed(
    ExecutionVariable Item,
    ExecutionVariable Index,
    ExecutionVariable Source,
    ExecutionRowAccessMode RowAccessMode,
    ExecutionBlock Body) : ExecutionNode;
