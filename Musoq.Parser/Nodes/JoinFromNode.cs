namespace Musoq.Parser.Nodes
{
    public class JoinFromNode : FromNode
    {
        public FromNode Source { get; }
        public FromNode With { get; }
        public Node Expression { get; }
        public JoinType JoinType { get; }
        public JoinFromNode(FromNode joinFrom, FromNode from, Node expression, JoinType joinType) 
            : base(string.Empty)
        {
            Source = joinFrom;
            With = @from;
            Expression = expression;
            JoinType = joinType;
        }

        public override string Alias => $"{Source.Alias}{With.Alias}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";

        public override string ToString()
        {
            return $"({Source.ToString()}, {With.ToString()}, {Expression.ToString()})";
        }
    }
}