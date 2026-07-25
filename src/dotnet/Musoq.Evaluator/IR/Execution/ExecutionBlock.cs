using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBlock
{
    private IReadOnlyList<ExecutionNode> _nodes = [];

    public ExecutionBlock(IReadOnlyList<ExecutionNode> nodes)
    {
        Nodes = ExecutionIrCollections.Freeze(nodes);
    }

    public IReadOnlyList<ExecutionNode> Nodes
    {
        get => _nodes;
        init => _nodes = ExecutionIrCollections.Freeze(value);
    }

    public static ExecutionBlock Empty { get; } = new([]);
}
