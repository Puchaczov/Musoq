namespace Musoq.Parser.Nodes.From;

public class ApplyFromNode : BinaryFromNode
{
    internal ApplyFromNode(FromNode source, FromNode with, ApplyType applyType, bool withOrdinality = false)
        : base(source, with, CreateAlias(source, with))
    {
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public ApplyFromNode(FromNode source, FromNode with, ApplyType applyType, Type returnType, bool withOrdinality = false)
        : base(source, with, CreateAlias(source, with), returnType)
    {
        ApplyType = applyType;
        WithOrdinality = withOrdinality;
    }

    public ApplyType ApplyType { get; }

    public bool WithOrdinality { get; }

    public override string Id => $"{nameof(ApplyFromNode)}{Source.Id}{With.Id}{(WithOrdinality ? "WithOrdinality" : string.Empty)}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var applyType = ApplyType == ApplyType.Cross ? "cross apply" : "outer apply";

        var ordinality = WithOrdinality ? " with ordinality" : string.Empty;

        return $"{Source.ToString()} {applyType} {With.ToString()}{ordinality}";
    }

    private static string CreateAlias(FromNode source, FromNode with)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(with);
        return $"{source.Alias}{with.Alias}";
    }
}
