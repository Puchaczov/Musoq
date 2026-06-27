using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionProjectTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    GeneratedRowShape RowShape,
    IReadOnlyList<int> FieldIndexes,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked) : ExecutionNode;
