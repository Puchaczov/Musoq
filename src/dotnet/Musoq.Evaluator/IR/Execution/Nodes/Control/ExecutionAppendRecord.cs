using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAppendRecord(
    ExecutionVariable List,
    GeneratedRecordShape RecordShape,
    IReadOnlyList<ExecutionRowValue> Values) : ExecutionNode;
