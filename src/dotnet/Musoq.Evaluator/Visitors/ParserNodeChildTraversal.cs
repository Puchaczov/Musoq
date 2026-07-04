using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ParserNodeChildTraversal
{
    public static void TraverseChildren(Node node, IExpressionVisitor visitor)
    {
        foreach (var child in EnumerateChildren(node))
            child.Accept(visitor);
    }

    public static void TraverseCteInnerExpressionsThenOuter(CteExpressionNode node, IExpressionVisitor visitor)
    {
        foreach (var child in ParserNodeTraversalRegistry.EnumerateCteInnerExpressionsThenOuter(node))
            child.Accept(visitor);
    }

    public static IEnumerable<Node> EnumerateChildren(Node node)
    {
        return ParserNodeTraversalRegistry.EnumerateChildren(node);
    }
}
