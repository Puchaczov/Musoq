namespace Musoq.Parser.Nodes
{
    public class JoinFromNode : FromNode
    {
        public JoinFromNode(FromNode joinFrom, FromNode from, Node expression, JoinType joinType)
            : base($"{joinFrom.Alias}{from.Alias}")
        {
            Source = joinFrom;
            With = from;
            Expression = expression;
            JoinType = joinType;
        }

        public FromNode Source { get; }
        public FromNode With { get; }
        public Node Expression { get; }
        public JoinType JoinType { get; }
        public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"({Source.ToString()}, {With.ToString()}, {Expression.ToString()})";
        }
    }
}