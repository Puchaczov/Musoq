﻿namespace Musoq.Parser.Nodes;

public class AddNode : BinaryNode
{
    public AddNode(Node left, Node right) : base(left, right)
    {
        Id = CalculateId(this);
    }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Left.ToString()} + {Right.ToString()}";
    }
}