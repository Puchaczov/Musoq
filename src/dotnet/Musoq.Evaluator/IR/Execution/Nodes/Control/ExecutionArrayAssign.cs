using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionArrayAssign(
    ExecutionVariable Array,
    ExecutionExpression Index,
    ExecutionExpression Value,
    Type ElementType) : ExecutionNode;
