namespace Musoq.Parser.Nodes.From;

public class JoinSourcesTableFromNode : FromNode
{
    internal JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType, FieldOrderedNode? tieBreak = null)
        : base(CreateAlias(first, second))
    {
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}{expression.ToString()}{tieBreak?.Id}";
        First = first;
        Second = second;
        Expression = expression;
        JoinType = joinType;
        TieBreak = tieBreak;
    }

    public JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType,
        Type returnType, FieldOrderedNode? tieBreak = null)
        : base(CreateAlias(first, second), returnType)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(expression);
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}{expression.ToString()}{tieBreak?.Id}";
        First = first;
        Second = second;
        Expression = expression;
        JoinType = joinType;
        TieBreak = tieBreak;
    }

    public Node Expression { get; }

    public FromNode First { get; }

    public FromNode Second { get; }

    public JoinType JoinType { get; }

    public FieldOrderedNode? TieBreak { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var joinType = JoinTypeSql.GetKeyword(JoinType);

        if (!JoinTypeSql.PrintsCondition(JoinType))
            return $"{First.ToString()} {joinType} {Second.ToString()}";

        var tieBreak = TieBreak == null
            ? string.Empty
            : $" tie break by {TieBreak.ToString()}";

        return $"{First.ToString()} {joinType} {Second.ToString()} on {Expression.ToString()}{tieBreak}";
    }

    private static string CreateAlias(FromNode first, FromNode second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return $"{first.Alias}{second.Alias}";
    }
}
