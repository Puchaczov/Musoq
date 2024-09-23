using System;

namespace Musoq.Parser.Nodes.From;

public class JoinNode : FromNode
{
    internal JoinNode(JoinFromNode join)
        : base(join.Alias)
    {
        Id = $"{nameof(JoinNode)}{join.Id}";
        Join = join;
    }
        
    public JoinNode(JoinFromNode join, Type returnType)
        : base(join.Alias, returnType)
    {
        Id = $"{nameof(JoinNode)}{join.Id}";
        Join = join;
    }

    public JoinFromNode Join { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Join.ToString();
    }
}