using System;

namespace Musoq.Parser.Nodes;

public class CteInnerExpressionNode : Node
{
    public CteInnerExpressionNode(Node value, string name)
    {
        Value = value;
        Name = name;
    }

    public Node Value { get; }

    public string Name { get; }

    public override Type ReturnType => typeof(void);

    public override string Id => $"{nameof(CteInnerExpressionNode)}{Value.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name} as {Value.ToString()}";
    }
}