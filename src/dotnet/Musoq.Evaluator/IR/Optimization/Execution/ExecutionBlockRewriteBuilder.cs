using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class ExecutionBlockRewriteBuilder(ExecutionBlock block, int additionalCapacity = 2)
{
    private List<ExecutionNode>? _nodes;

    public bool HasChanges => _nodes != null;

    public IReadOnlyList<ExecutionNode> Nodes => _nodes ?? block.Nodes;

    public int Count => Nodes.Count;

    public ExecutionNode this[int index] => Nodes[index];

    public void EnsureStartedAt(int currentIndex)
    {
        if (_nodes != null)
            return;

        _nodes = new List<ExecutionNode>(block.Nodes.Count + additionalCapacity);
        for (var index = 0; index < currentIndex; index++)
            _nodes.Add(block.Nodes[index]);
    }

    public void Add(ExecutionNode node)
    {
        StartedNodes.Add(node);
    }

    public void AddRange(IEnumerable<ExecutionNode> nodes)
    {
        StartedNodes.AddRange(nodes);
    }

    public void InsertRange(int index, IEnumerable<ExecutionNode> nodes)
    {
        StartedNodes.InsertRange(index, nodes);
    }

    public ExecutionBlock ToBlock()
    {
        return _nodes == null
            ? block
            : block with { Nodes = _nodes };
    }

    private List<ExecutionNode> StartedNodes =>
        _nodes ?? throw new InvalidOperationException("Execution block rewrite has not started.");
}
