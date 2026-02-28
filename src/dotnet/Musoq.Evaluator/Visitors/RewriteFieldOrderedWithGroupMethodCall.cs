using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldOrderedWithGroupMethodCall
    : RewriteFieldWithGroupMethodCallBase<FieldOrderedNode, FieldNode>
{
    private readonly FieldNode[] _groupByFields;

    public RewriteFieldOrderedWithGroupMethodCall(FieldNode[] nodes) : base(nodes)
    {
        _groupByFields = nodes;
    }

    public override void Visit(FieldOrderedNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldOrderedNode;
    }

    public override void Visit(AccessMethodNode node)
    {
        if (node.IsAggregateMethod())
        {
            base.Visit(node);
            return;
        }

        var nodeString = node.ToString();

        if (MatchesGroupByField(nodeString))
        {
            Nodes.Push(new AccessColumnNode(nodeString, string.Empty, node.ReturnType, TextSpan.Empty));
            return;
        }

        base.Visit(node);
    }

    protected override string ExtractOriginalExpression(FieldNode node)
    {
        return node.FieldName;
    }

    private bool MatchesGroupByField(string expression)
    {
        return _groupByFields.Any(f => f.FieldName == expression || f.Expression.ToString() == expression);
    }
}
