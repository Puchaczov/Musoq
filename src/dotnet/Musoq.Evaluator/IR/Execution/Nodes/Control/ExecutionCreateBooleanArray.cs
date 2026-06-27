using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCreateBooleanArray(
    ExecutionVariable Array,
    ExecutionVariable LengthSource) : ExecutionNode;
