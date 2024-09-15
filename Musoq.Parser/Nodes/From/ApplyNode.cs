using System;

namespace Musoq.Parser.Nodes.From;

public class ApplyNode : FromNode
{
    internal ApplyNode(ApplyFromNode apply)
        : base(apply.Alias)
    {
        Id = $"{nameof(ApplyNode)}{apply.Id}";
        Apply = apply;
    }
        
    public ApplyNode(ApplyFromNode apply, Type returnType)
        : base(apply.Alias, returnType)
    {
        Id = $"{nameof(JoinsNode)}{apply.Id}";
        Apply = apply;
    }

    public ApplyFromNode Apply { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Apply.ToString();
    }
}