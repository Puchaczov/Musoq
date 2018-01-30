namespace Musoq.Parser.Nodes
{
    public class EqualityNode : BinaryNode
    {
        public EqualityNode(Node left, Node right) : base(left, right)
        {
            CalculateId(this);
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} = {Right.ToString()}";
        }
    }
}