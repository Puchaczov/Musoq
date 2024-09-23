using System;

namespace Musoq.Parser.Nodes;

public class AndNode : BinaryNode
{
    public AndNode(Node left, Node right)
        : base(left, right)
    {
        Id = CalculateId(this);
    }

    public override Type ReturnType => typeof(bool);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Left.ToString()} and {Right.ToString()}";
    }
}