using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelBlock(
    string Name,
    int MaxDegreeOfParallelism,
    IReadOnlyList<ExecutionParallelTask> Tasks,
    ExecutionParallelMerge Merge) : ExecutionNode;
