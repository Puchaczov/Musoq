using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(InQueryNode node)
    {
        var subquery = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new InQueryNode(left, subquery).WithSpan(node.Span).WithFullSpan(node.FullSpan));
    }

    public override void Visit(ExistsQueryNode node)
    {
        var subquery = Nodes.Pop();
        Nodes.Push(new ExistsQueryNode(subquery).WithSpan(node.Span).WithFullSpan(node.FullSpan));
    }

    public override void Visit(ScalarSubqueryNode node)
    {
        var subquery = Nodes.Pop();
        Nodes.Push(new ScalarSubqueryNode(subquery).WithSpan(node.Span).WithFullSpan(node.FullSpan));
    }
}
