using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>A borrowed child region; singleton and list regions need no synthetic block.</summary>
internal readonly record struct ExecutionChildRegion
{
    private readonly ExecutionBlock? _block;
    private readonly IReadOnlyList<ExecutionNode>? _nodes;
    private readonly ExecutionNode? _node;

    internal ExecutionChildRegion(ExecutionBlock block) => _block = block;
    internal ExecutionChildRegion(IReadOnlyList<ExecutionNode> nodes) => _nodes = nodes;
    internal ExecutionChildRegion(ExecutionNode node) => _node = node;

    internal bool IsPresent => _block is not null || _nodes is not null || _node is not null;
    internal int Count => _block?.Nodes.Count ?? _nodes?.Count ?? (_node is null ? 0 : 1);
    internal ExecutionNode this[int index] => _block?.Nodes[index] ?? _nodes?[index] ??
        (index == 0 && _node is not null ? _node : throw new ArgumentOutOfRangeException(nameof(index)));

    internal ExecutionBlock AsBlock()
    {
        if (_block is not null)
            return _block;
        if (_nodes is not null)
            return new ExecutionBlock(_nodes);
        if (_node is not null)
            return new ExecutionBlock([_node]);
        throw new InvalidOperationException("An absent child region has no block.");
    }
}
