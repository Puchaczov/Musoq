namespace Musoq.Parser.Nodes
{
    public class JoinSourcesTableFromNode : FromNode
    {
        public JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression)
            : base($"{first.Alias}{second.Alias}")
        {
            Id = $"{nameof(JoinSourcesTableFromNode)}{Alias}{expression.ToString()}";
            First = first;
            Second = second;
            Expression = expression;
        }

        public Node Expression { get; }

        public FromNode First { get; }

        public FromNode Second { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"from ({First.ToString()}, {Second.ToString()}, {Expression.ToString()})";
        }
    }
}