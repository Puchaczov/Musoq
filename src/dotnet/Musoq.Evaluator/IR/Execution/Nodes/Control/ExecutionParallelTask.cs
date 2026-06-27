using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelTask(
    string Name,
    ExecutionVariable Output,
    ExecutionBlock Body,
    int? RelatedTableIndex = null,
    string? RelatedQueryIdentifier = null);
