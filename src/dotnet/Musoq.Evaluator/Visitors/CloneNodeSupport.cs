using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticNodeCloneSupport
{
    public static void Visit(Node node, Stack<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(nodes);
        if (node is not InMemoryGroupedFromNode grouped)
            throw new NotSupportedException($"Cannot clone semantic node '{node.GetType().FullName}'.");

        var clone = new InMemoryGroupedFromNode(grouped.Alias, grouped.ReturnType ?? typeof(object));
        if (grouped.HasSpan)
            clone.WithSpan(grouped.Span);
        if (!grouped.FullSpan.IsEmpty)
            clone.WithFullSpan(grouped.FullSpan);
        nodes.Push(clone);
    }
}
