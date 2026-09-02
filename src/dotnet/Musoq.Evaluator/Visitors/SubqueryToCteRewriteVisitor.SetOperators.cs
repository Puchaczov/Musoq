using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    public override void Visit(UnionNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(UnionAllNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(ExceptNode node)
    {
        RewriteSetOperator(node);
    }

    public override void Visit(IntersectNode node)
    {
        RewriteSetOperator(node);
    }

}
