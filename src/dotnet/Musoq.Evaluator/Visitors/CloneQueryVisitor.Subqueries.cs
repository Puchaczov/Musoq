using Musoq.Parser.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(InQueryNode node)
    {
        var subquery = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new InQueryNode(left, subquery).WithSpan(node.Span).WithFullSpan(node.FullSpan));
    }

    public override void Visit(ExistsQueryNode node) => Nodes.Push(PredicateSubqueryCloner.Clone(node, Nodes));

    public override void Visit(ScalarSubqueryNode node)
    {
        Nodes.Push(SubqueryApplyCloner.Clone(node, Nodes));
    }
}
