namespace Musoq.Parser.Nodes.From;

public class ApplyNode : FromNode
{
    internal ApplyNode(ApplyFromNode apply)
        : base(GetAlias(apply))
    {
        Id = $"{nameof(ApplyNode)}{apply.Id}";
        Apply = apply;
    }

    public ApplyNode(ApplyFromNode apply, Type returnType)
        : base(GetAlias(apply), returnType)
    {
        ArgumentNullException.ThrowIfNull(apply);
        Id = $"{nameof(ApplyNode)}{apply.Id}";
        Apply = apply;
    }

    public ApplyFromNode Apply { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Apply.ToString();
    }

    private static string GetAlias(ApplyFromNode apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        return apply.Alias;
    }
}
