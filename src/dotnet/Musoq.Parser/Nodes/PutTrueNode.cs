using System;

namespace Musoq.Parser.Nodes;

public class PutTrueNode : Node
{
    public override Type ReturnType => typeof(bool);

    public override string Id => $"{nameof(PutTrueNode)}true";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return "1 = 1";
    }
}
