using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionForEachWithOrdinality(
    ExecutionVariable Item,
    ExecutionExpression Source,
    ExecutionVariable Ordinal,
    ExecutionBlock Body) : ExecutionNode;
