namespace Musoq.Parser.Nodes
{
    public abstract class JoinNode : BinaryNode
    {
        protected JoinNode(FromNode left, Node right)
            : base(left, right)
        {
        }

        public FromNode From => (FromNode) Left;

        public Node Expression => Right;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}