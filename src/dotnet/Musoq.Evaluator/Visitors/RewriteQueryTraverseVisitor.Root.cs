using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor
{
    public override void Visit(RootNode node)
    {
        if (Visitor is RewriteQueryVisitor rewrite)
            rewrite.IntermediateSources.ReserveNames(node);
        VisitChildrenThenNode(node);
    }
}
