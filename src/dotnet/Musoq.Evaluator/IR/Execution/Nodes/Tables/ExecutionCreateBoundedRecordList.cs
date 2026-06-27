using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateBoundedRecordList(
    ExecutionVariable List,
    GeneratedRecordShape RecordShape,
    IReadOnlyList<ExecutionOrderField> Keys,
    ExecutionOrderRecordSelection Selection) : ExecutionNode;
