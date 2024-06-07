using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewriteFieldOrderedWithGroupMethodCall : RewriteFieldWithGroupMethodCallBase<FieldOrderedNode>
{
    public RewriteFieldOrderedWithGroupMethodCall(FieldNode[] nodes) 
        : base(nodes)
    {
    }
        
    public override void Visit(FieldOrderedNode node)
    {
        base.Visit(node);
        Expression = Nodes.Pop() as FieldOrderedNode;
    }
}