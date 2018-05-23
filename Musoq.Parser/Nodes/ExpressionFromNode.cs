namespace Musoq.Parser.Nodes
{
    public class ExpressionFromNode : FromNode
    {
        public ExpressionFromNode(FromNode from)
            : base(from.Alias)
        {
            Expression = from;
            Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
        }

        public FromNode Expression { get; }

        public override string Alias => Expression.Alias;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"from ({Expression.ToString()})";
        }
    }
}