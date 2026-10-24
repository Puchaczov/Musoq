using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticNodeCloneSupport
{
    internal static MultiStatementNode CloneStatements(MultiStatementNode node, Node[] items)
    {
        if (node is Parser.IntermediateStatementsNode bound)
            return new Parser.IntermediateStatementsNode(items, node.ReturnType, bound.Producers);

        return new MultiStatementNode(items, node.ReturnType);
    }

    internal static void CloneShortCircuit(Node node, Stack<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(node);
        nodes.Push(node switch
        {
            ShortCircuitingNodeLeft left => new ShortCircuitingNodeLeft(nodes.Pop(), left.UsedFor),
            ShortCircuitingNodeRight right => new ShortCircuitingNodeRight(nodes.Pop(), right.UsedFor),
            _ => throw new ArgumentException("Expected a short-circuit node.", nameof(node))
        });
    }

    public static void Visit(Node node, Stack<Node> nodes)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(nodes);
        if (node is not InMemoryGroupedFromNode grouped)
            throw new NotSupportedException($"Cannot clone semantic node '{node.GetType().FullName}'.");

        InMemoryGroupedFromNode clone = grouped is Parser.InMemoryGroupedFromNode bound
            ? new Parser.InMemoryGroupedFromNode(grouped.Alias) { IntermediateSource = bound.IntermediateSource }
            : new InMemoryGroupedFromNode(grouped.Alias, grouped.ReturnType ?? typeof(object));
        if (grouped.HasSpan)
            clone.WithSpan(grouped.Span);
        if (!grouped.FullSpan.IsEmpty)
            clone.WithFullSpan(grouped.FullSpan);
        nodes.Push(clone);
    }
}
