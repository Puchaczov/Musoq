using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionEnsureTableCapacity(
    ExecutionVariable Table,
    ExecutionCapacityHint CapacityHint) : ExecutionNode;
