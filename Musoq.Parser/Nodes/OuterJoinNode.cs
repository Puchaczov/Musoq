namespace Musoq.Parser.Nodes
{
    public class OuterJoinNode : BinaryNode
    {
        public enum OuterJoinType
        {
            Left,
            Right
        }

        public OuterJoinNode(OuterJoinType outerJoinType, Node from, Node expression)
            : base(from, expression)
        {
            Id = CalculateId(this);
            Type = outerJoinType;
        }

        public OuterJoinType Type { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            var typeString = Type == OuterJoinType.Left ? "left" : "right";
            return $"{typeString} outer join {Left.ToString()} on {Right.ToString()}";
        }
    }
}