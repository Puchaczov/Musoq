using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMaterializeRecordListToTable(
    ExecutionVariable Source,
    ExecutionVariable Target,
    GeneratedRecordShape RecordShape,
    GeneratedRowShape RowShape,
    IReadOnlyList<int> FieldIndexes,
    IReadOnlyList<int> RenumberFieldIndexes,
    ExecutionCapacityHint? CapacityHint = null,
    ExecutionAppendMode AppendMode = ExecutionAppendMode.Checked) : ExecutionNode;
