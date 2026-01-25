using System;

namespace Musoq.Parser.Nodes.From;

public class ReferentialFromNode : FromNode
{
    internal ReferentialFromNode(string name, string alias)
        : base(alias)
    {
        Name = name;
    }

    public ReferentialFromNode(string name, string alias, Type returnType)
        : base(alias, returnType)
    {
        Name = name;
    }

    public string Name { get; }

    public override string Id => $"{Name}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name}";
    }
}
