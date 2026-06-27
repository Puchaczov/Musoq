using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionSortTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    IReadOnlyList<ExecutionOrderField> Keys,
    IReadOnlyList<int> RenumberFieldIndexes,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked,
    ExecutionColumnMetadata? ColumnMetadata = null) : ExecutionNode
{
    public ExecutionSortTable(
        ExecutionVariable source,
        ExecutionVariable target,
        IReadOnlyList<ExecutionOrderField> keys)
        : this(source, target, keys, [])
    {
    }
}
