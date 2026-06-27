namespace Musoq.Parser.Nodes;

public class IntoNode(string name) : Node
{
    public string Name { get; } = name;

    public override Type? ReturnType => null;

    public override string Id { get; } = $"{nameof(IntoNode)}{name}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"into {Name}";
    }
}
