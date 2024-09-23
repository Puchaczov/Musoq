using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class RawFunctionNode : Node
{
    public RawFunctionNode(Node[] args)
    {
        Parameters = args;
        Id = $"{nameof(RawFunctionNode)}{args.Select(f => f.Id).Aggregate((a, b) => a + b)}";
    }

    public Node[] Parameters { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return null;
    }
}