using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor
{
    public override void Visit(InternalQueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _walker = _walker.NextChild();

        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
    }

    public override void Visit(DescNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        VisitChildrenThenNode(node);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(UnionNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(UnionAllNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(ExceptNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(IntersectNode node)
    {
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        ParserNodeChildTraversal.TraverseCteInnerExpressionsThenOuter(node, this);
        node.Accept(Visitor);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        _walker = _walker.NextChild();
        Visitor.SetScope(_walker.Scope);

        VisitChildrenThenNode(node);

        _walker = _walker.Parent();
        Visitor.SetScope(_walker.Scope);
    }

    private void TraverseSetOperatorWithScope(SetOperatorNode node)
    {
        _walker = _walker.NextChild();
        VisitChildrenThenNode(node);
        _walker = _walker.Parent();
    }
}
