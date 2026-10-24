using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class SubqueryWindowCloner
{
    internal static WindowSpecificationNode Clone(WindowSpecificationNode node, Stack<Node> nodes)
    {
        WindowFrameNode? frame = null;
        if (node.Frame is not null)
            frame = (WindowFrameNode)nodes.Pop();
        var order = new FieldOrderedNode[node.OrderByFields.Length];
        for (var index = order.Length - 1; index >= 0; index--)
            order[index] = (FieldOrderedNode)nodes.Pop();
        var partition = new FieldNode[node.PartitionFields.Length];
        for (var index = partition.Length - 1; index >= 0; index--)
            partition[index] = (FieldNode)nodes.Pop();
        return new WindowSpecificationNode(partition, order, frame).CopySpansFrom(node);
    }

    internal static WindowFunctionNode Clone(WindowFunctionNode node, Stack<Node> nodes)
    {
        WindowSpecificationNode? specification = null;
        if (node.WindowSpecification is not null)
            specification = (WindowSpecificationNode)nodes.Pop();
        var function = (AccessMethodNode)nodes.Pop();
        if (node.IsNamedWindowReference)
            return new WindowFunctionNode(function,
                node.WindowName ?? throw new InvalidOperationException("Named window reference requires a name.")).CopySpansFrom(node);
        return new WindowFunctionNode(function,
            specification ?? throw new InvalidOperationException("Window function requires a specification.")).CopySpansFrom(node);
    }
}
