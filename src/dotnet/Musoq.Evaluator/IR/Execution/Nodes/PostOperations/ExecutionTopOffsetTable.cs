using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionTopOffsetTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    IReadOnlyList<ExecutionOrderField> Keys,
    int SkipCount,
    int TakeCount,
    IReadOnlyList<int> RenumberFieldIndexes,
    ExecutionTopOffsetStrategy Strategy,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode;
