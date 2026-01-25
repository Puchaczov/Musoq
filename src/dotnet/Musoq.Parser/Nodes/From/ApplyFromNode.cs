using System;

namespace Musoq.Parser.Nodes.From;

public class ApplyFromNode : BinaryFromNode
{
    internal ApplyFromNode(FromNode source, FromNode with, ApplyType applyType)
        : base(source, with, $"{source.Alias}{with.Alias}")
    {
        ApplyType = applyType;
    }

    public ApplyFromNode(FromNode source, FromNode with, ApplyType applyType, Type returnType)
        : base(source, with, $"{source.Alias}{with.Alias}", returnType)
    {
        ApplyType = applyType;
    }

    public ApplyType ApplyType { get; }
    public override string Id => $"{nameof(ApplyFromNode)}{Source.Id}{With.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var applyType = ApplyType == ApplyType.Cross ? "cross apply" : "outer apply";

        return $"{Source.ToString()} {applyType} {With.ToString()}";
    }
}
