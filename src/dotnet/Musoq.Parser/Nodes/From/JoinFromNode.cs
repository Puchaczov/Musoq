using System;

namespace Musoq.Parser.Nodes.From;

public class JoinFromNode : BinaryFromNode
{
    internal JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType)
        : base(source, with, $"{source.Alias}{with.Alias}")
    {
        Expression = expression;
        JoinType = joinType;
    }

    public JoinFromNode(FromNode source, FromNode with, Node expression, JoinType joinType, Type returnType)
        : base(source, with, $"{source.Alias}{with.Alias}", returnType)
    {
        Expression = expression;
        JoinType = joinType;
    }

    public Node Expression { get; }
    public JoinType JoinType { get; }
    public override string Id => $"{nameof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var joinType = JoinType switch
        {
            JoinType.Inner => "inner join",
            JoinType.OuterLeft => "left outer join",
            _ => "right outer join"
        };

        return $"{Source.ToString()} {joinType} {With.ToString()} on {Expression.ToString()}";
    }
}
