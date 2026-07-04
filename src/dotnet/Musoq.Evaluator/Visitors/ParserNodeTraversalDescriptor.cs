using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal enum ParserNodeTraversalMode
{
    Leaf,
    Children,
    SpecialOrder,
    Unsupported
}

internal sealed record ParserNodeTraversalDescriptor(
    Type NodeType,
    ParserNodeTraversalMode Mode,
    bool IncludeDerivedTypes,
    Func<Node, IEnumerable<Node>> EnumerateChildren)
{
    public bool Covers(Type nodeType)
    {
        return IncludeDerivedTypes
            ? NodeType.IsAssignableFrom(nodeType)
            : NodeType == nodeType;
    }

    public IEnumerable<Node> Enumerate(Node node)
    {
        if (!Covers(node.GetType()))
            throw new InvalidOperationException(
                $"Traversal descriptor for {NodeType.Name} cannot enumerate {node.GetType().Name}.");

        return EnumerateChildren(node);
    }
}
