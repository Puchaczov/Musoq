namespace Musoq.Parser.Nodes
{
    public class JoinInMemoryWithSourceTableFromNode : FromNode
    {
        public JoinInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, Node expression)
            : base($"{inMemoryTableAlias}{sourceTable.Alias}")
        {
            Id =
                $"{nameof(JoinInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{expression.ToString()}";
            InMemoryTableAlias = inMemoryTableAlias;
            SourceTable = sourceTable;
            Expression = expression;
        }

        public string InMemoryTableAlias { get; }

        public FromNode SourceTable { get; }

        public Node Expression { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"join {InMemoryTableAlias} with {SourceTable.Alias} on {Expression.ToString()}";
        }
    }
}