namespace Musoq.Parser.Nodes.From;

public class JoinInMemoryWithSourceTableFromNode(
    string inMemoryTableAlias,
    FromNode sourceTable,
    Node expression,
    JoinType joinType,
    Type returnType,
    FieldOrderedNode? tieBreak = null)
    : FromNode($"{inMemoryTableAlias}{sourceTable.Alias}", returnType)
{
    public string InMemoryTableAlias { get; } = inMemoryTableAlias;

    public FromNode SourceTable { get; } = sourceTable;

    public Node Expression { get; } = expression;

    public FieldOrderedNode? TieBreak { get; } = tieBreak;

    public override string Id { get; } = $"{nameof(JoinInMemoryWithSourceTableFromNode)}{inMemoryTableAlias}{sourceTable.Alias}{expression.ToString()}{tieBreak?.Id}";

    public JoinType JoinType { get; } = joinType;

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var tieBreakClause = TieBreak == null
            ? string.Empty
            : $" tie break by {TieBreak.ToString()}";

        return $"join {InMemoryTableAlias} with {SourceTable.Alias} on {Expression.ToString()}{tieBreakClause}";
    }
}
