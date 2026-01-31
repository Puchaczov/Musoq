using System;

namespace Musoq.Parser.Nodes;

public class IdentifierNode : Node
{
    public IdentifierNode(string name, Type returnType = null)
        : this(name, returnType, default)
    {
    }

    public IdentifierNode(string name, Type returnType, TextSpan span)
    {
        Name = name;
        ReturnType = returnType;
        Id = $"{nameof(IdentifierNode)}{Name}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }
    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
