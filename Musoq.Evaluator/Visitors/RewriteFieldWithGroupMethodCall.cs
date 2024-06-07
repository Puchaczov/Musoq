using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteFieldWithGroupMethodCall : RewriteFieldWithGroupMethodCallBase<FieldNode>
    {
        public RewriteFieldWithGroupMethodCall(FieldNode[] nodes) 
            : base(nodes)
        {
        }
        
        public override void Visit(FieldNode node)
        {
            base.Visit(node);
            Expression = Nodes.Pop() as FieldNode;
        }
    }
}