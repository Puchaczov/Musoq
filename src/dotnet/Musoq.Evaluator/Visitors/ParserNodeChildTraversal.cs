using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ParserNodeChildTraversal
{
    internal static bool Contains<TNode>(Node node)
        where TNode : Node => node is TNode || EnumerateChildren(node).Any(Contains<TNode>);

    internal static bool ContainsProjectionCall(Node node) =>
        node is AccessMethodNode or WindowFunctionNode or ScalarSubqueryNode or InQueryNode or ExistsQueryNode
        || EnumerateChildren(node).Any(ContainsProjectionCall);

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
