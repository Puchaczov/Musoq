namespace Musoq.Parser.Nodes.From;

public class ExpressionFromNode : FromNode
{
    internal ExpressionFromNode(FromNode from)
        : base(GetAlias(from))
    {
        Expression = from;
        Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
    }

    public ExpressionFromNode(FromNode from, Type returnType)
        : base(GetAlias(from), returnType)
    {
        ArgumentNullException.ThrowIfNull(from);
        Expression = from;
        Id = $"{nameof(ExpressionFromNode)}{from.ToString()}";
    }

    public FromNode Expression { get; }

    public override string Alias => Expression.Alias;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"from {Expression.ToString()}";
    }

    private static string GetAlias(FromNode from)
    {
        ArgumentNullException.ThrowIfNull(from);
        return from.Alias;
    }
}
