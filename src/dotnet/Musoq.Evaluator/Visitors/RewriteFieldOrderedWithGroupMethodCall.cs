using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldOrderedWithGroupMethodCall(FieldNode[] nodes)
    : RewriteFieldWithGroupMethodCallBase<FieldOrderedNode, FieldNode>(nodes)
{
    private readonly FieldNode[] _groupByFields = nodes;

    public override void Visit(FieldOrderedNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldOrderedNode
                     ?? throw new InvalidOperationException("Expected a rewritten ordered field node.");
    }

    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsAggregateMethod())
        {
            base.Visit(node);
            return;
        }

        var nodeString = node.ToString();

        if (MatchesGroupByField(nodeString))
        {
            Nodes.Pop();
            Nodes.Push(new AccessColumnNode(nodeString, string.Empty, node.ReturnType, TextSpan.Empty));
            return;
        }

        base.Visit(node);
    }

    protected override string ExtractOriginalExpression(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.FieldName;
    }

    private bool MatchesGroupByField(string expression)
    {
        return _groupByFields.Any(f => f.FieldName == expression || f.Expression.ToString() == expression);
    }
}
