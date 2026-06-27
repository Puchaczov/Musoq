namespace Musoq.Parser.Nodes.From;

public class ApplySourcesTableFromNode : FromNode
{
    internal ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType, bool withOrdinality = false)
        : base(CreateAlias(first, second))
    {
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}{(withOrdinality ? "WithOrdinality" : string.Empty)}";
        First = first;
        Second = second;
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType, Type returnType, bool withOrdinality = false)
        : base(CreateAlias(first, second), returnType)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}{(withOrdinality ? "WithOrdinality" : string.Empty)}";
        First = first;
        Second = second;
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public FromNode First { get; }

    public FromNode Second { get; }

    public ApplyType ApplyType { get; }

    public bool WithOrdinality { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var joinType = ApplyType == ApplyType.Cross ? "cross apply" : "outer apply";

        var ordinality = WithOrdinality ? " with ordinality" : string.Empty;

        return $"{First.ToString()} {joinType} {Second.ToString()}{ordinality}";
    }

    private static string CreateAlias(FromNode first, FromNode second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return $"{first.Alias}{second.Alias}";
    }
}
