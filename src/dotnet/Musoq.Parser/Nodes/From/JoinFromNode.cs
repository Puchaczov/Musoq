namespace Musoq.Parser.Nodes.From;

public class JoinFromNode : BinaryFromNode
{
    internal JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType, FieldOrderedNode? tieBreak = null)
        : base(source, with, CreateAlias(source, with))
    {
        Expression = expression;
        JoinType = joinType;
        TieBreak = tieBreak;
    }

    public JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType, Type returnType, FieldOrderedNode? tieBreak = null)
        : base(source, with, CreateAlias(source, with), returnType)
    {
        Expression = expression;
        JoinType = joinType;
        TieBreak = tieBreak;
    }

    public Node Expression { get; }
    public JoinType JoinType { get; }
    public FieldOrderedNode? TieBreak { get; }
    public override string Id => $"{nameof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}{TieBreak?.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var joinType = JoinTypeSql.GetKeyword(JoinType);

        if (!JoinTypeSql.PrintsCondition(JoinType))
            return $"{Source.ToString()} {joinType} {With.ToString()}";

        var tieBreak = TieBreak == null
            ? string.Empty
            : $" tie break by {TieBreak.ToString()}";

        return $"{Source.ToString()} {joinType} {With.ToString()} on {Expression.ToString()}{tieBreak}";
    }

    private static string CreateAlias(FromNode source, FromNode with)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(with);
        return $"{source.Alias}{with.Alias}";
    }
}
