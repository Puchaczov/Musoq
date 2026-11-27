namespace Musoq.Parser.Nodes.From;

public class ExpressionFromNode : FromNode
{
    internal ExpressionFromNode(FromNode from)
        : base(GetAlias(from))
    {
        Expression = from;
        Id = GetId(from);
    }

    public ExpressionFromNode(FromNode from, Type returnType)
        : base(GetAlias(from), returnType)
    {
        ArgumentNullException.ThrowIfNull(from);
        Expression = from;
        Id = GetId(from);
    }

    public FromNode Expression { get; }

    public override string Alias => Expression.Alias;

    public override string Id { get; }

    private static string GetId(FromNode from) =>
        $"{nameof(ExpressionFromNode)}{from.ToString()}:{InlineSourceIdentity(from)}";

    // SQL display keeps the join kind; inline source identity also keeps literal types.
    private static string InlineSourceIdentity(FromNode source) =>
        source switch
        {
            ValuesFromNode or DerivedTableFromNode => source.Id,
            JoinNode join => InlineSourceIdentity(join.Join),
            ApplyNode apply => InlineSourceIdentity(apply.Apply),
            BinaryFromNode binary => $"({InlineSourceIdentity(binary.Source)},{InlineSourceIdentity(binary.With)})",
            ExpressionFromNode expression => InlineSourceIdentity(expression.Expression),
            UnpivotFromNode unpivot => InlineSourceIdentity(unpivot.Source),
            _ => string.Empty,
        };

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
