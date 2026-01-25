using System;

namespace Musoq.Parser.Nodes.From;

public class ApplySourcesTableFromNode : FromNode
{
    internal ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType)
        : base($"{first.Alias}{second.Alias}")
    {
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}";
        First = first;
        Second = second;
        ApplyType = applyType;
    }

    public ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType, Type returnType)
        : base($"{first.Alias}{second.Alias}", returnType)
    {
        Id = $"{nameof(JoinSourcesTableFromNode)}{first.Alias}{second.Alias}";
        First = first;
        Second = second;
        ApplyType = applyType;
    }

    public FromNode First { get; }

    public FromNode Second { get; }

    public ApplyType ApplyType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var joinType = ApplyType == ApplyType.Cross ? "cross apply" : "outer apply";

        return $"{First.ToString()} {joinType} {Second.ToString()}";
    }
}
