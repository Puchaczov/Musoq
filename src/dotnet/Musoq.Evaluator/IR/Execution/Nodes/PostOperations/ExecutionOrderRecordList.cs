using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionOrderRecordList(
    ExecutionVariable Source,
    GeneratedRecordShape RecordShape,
    IReadOnlyList<ExecutionOrderField> Keys,
    ExecutionOrderRecordSelection Selection) : ExecutionNode;
