using System;

namespace Musoq.Parser.Nodes;

public class AccessRawIdentifierNode : IdentifierNode
{
    public AccessRawIdentifierNode(string name, Type returnType = null)
        : base(name, returnType)
    {
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}