namespace Musoq.Parser.Nodes.From;

public class JoinNode : FromNode
{
    internal JoinNode(JoinFromNode join)
        : base(GetAlias(join))
    {
        Id = $"{nameof(JoinNode)}{join.Id}";
        Join = join;
    }

    public JoinNode(JoinFromNode join, Type returnType)
        : base(GetAlias(join), returnType)
    {
        ArgumentNullException.ThrowIfNull(join);
        Id = $"{nameof(JoinNode)}{join.Id}";
        Join = join;
    }

    public JoinFromNode Join { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Join.ToString();
    }

    private static string GetAlias(JoinFromNode join)
    {
        ArgumentNullException.ThrowIfNull(join);
        return join.Alias;
    }
}
