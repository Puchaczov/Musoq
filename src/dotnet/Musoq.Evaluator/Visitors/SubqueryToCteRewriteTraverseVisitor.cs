using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class SubqueryToCteRewriteTraverseVisitor(SubqueryToCteRewriteVisitor visitor) : CloneTraverseVisitor(visitor)
{
    public RootNode Root => visitor.Root;

    public override void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        visitor.RegisterReservedCteNames(node.InnerExpression);
        base.Visit(node);
    }
}
