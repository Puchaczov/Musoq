using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTopNTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    IReadOnlyList<ExecutionOrderField> Keys,
    int Count,
    IReadOnlyList<int> RenumberFieldIndexes,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode;
