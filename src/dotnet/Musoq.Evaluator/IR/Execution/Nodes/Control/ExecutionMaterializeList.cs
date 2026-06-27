using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionMaterializeList(
    ExecutionExpression Source,
    ExecutionVariable Buffer,
    GeneratedRowShape? GeneratedRowShape = null) : ExecutionNode;
