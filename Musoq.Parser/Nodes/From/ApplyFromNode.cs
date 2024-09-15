using System;

namespace Musoq.Parser.Nodes.From;

public class ApplyFromNode : FromNode
{
    internal ApplyFromNode(FromNode source, FromNode with, ApplyType applyType)
        : base($"{source.Alias}{with.Alias}")
    {
        Source = source;
        With = with;
        ApplyType = applyType;
    }
        
    public ApplyFromNode(FromNode source, FromNode with, ApplyType applyType, Type returnType)
        : base($"{source.Alias}{with.Alias}", returnType)
    {
        Source = source;
        With = with;
        ApplyType = applyType;
    }

    public FromNode Source { get; }
    public FromNode With { get; }
    public ApplyType ApplyType { get; }
    public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}";

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