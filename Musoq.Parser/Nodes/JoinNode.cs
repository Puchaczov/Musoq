namespace Musoq.Parser.Nodes
{
    public abstract class JoinNode : BinaryNode
    {
        protected JoinNode(Node left, Node right) : base(left, right)
        {
        }
    }
}