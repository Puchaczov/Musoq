namespace Musoq.Parser.Nodes;

public class CteInnerExpressionNode(Node value, string name) : Node
{
    public Node Value { get; } = value;

    public string Name { get; } = name;

    public override Type ReturnType => typeof(void);

    public override string Id => $"{nameof(CteInnerExpressionNode)}{Value.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name} as {Value.ToString()}";
    }
}
