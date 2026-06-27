using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionContinueIf(
    ExecutionExpression Condition) : ExecutionNode;
