using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMaterializeExpandoList(
    ExecutionExpression Source,
    ExecutionVariable Buffer,
    ExpandoAdapterShape Shape,
    ExecutionExpression? Predicate) : ExecutionNode;
