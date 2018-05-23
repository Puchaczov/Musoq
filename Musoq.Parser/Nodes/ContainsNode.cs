namespace Musoq.Parser.Nodes
{
    public class ContainsNode : BinaryNode
    {
        public ContainsNode(Node left, ArgsListNode right)
            : base(left, right)
        {
            ToCompareExpression = right;
            Id = CalculateId(this);
        }

        public ArgsListNode ToCompareExpression { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Left.ToString()} contains {Right.ToString()}";
        }
    }
}