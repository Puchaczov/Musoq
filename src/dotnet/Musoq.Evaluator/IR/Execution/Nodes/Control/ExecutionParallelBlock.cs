using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionParallelBlock : ExecutionNode
{
    public ExecutionParallelBlock(
        string name,
        int maxDegreeOfParallelism,
        IReadOnlyList<ExecutionParallelTask> tasks,
        ExecutionParallelMerge merge)
    {
        Name = name;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        Tasks = ExecutionIrCollections.Freeze(tasks);
        Merge = merge;
    }

    public string Name { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
    public IReadOnlyList<ExecutionParallelTask> Tasks { get; init; }
    public ExecutionParallelMerge Merge { get; init; }
}
