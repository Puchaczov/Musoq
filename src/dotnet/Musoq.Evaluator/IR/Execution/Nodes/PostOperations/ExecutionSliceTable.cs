using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSliceTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    int SkipCount,
    int TakeCount,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode;
