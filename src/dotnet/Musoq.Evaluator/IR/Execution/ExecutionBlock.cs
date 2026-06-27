using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBlock(IReadOnlyList<ExecutionNode> Nodes)
{
    public static ExecutionBlock Empty { get; } = new([]);
}
