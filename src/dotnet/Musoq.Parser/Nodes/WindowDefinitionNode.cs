namespace Musoq.Parser.Nodes;

public class WindowDefinitionNode(string name, WindowSpecificationNode specification) : Node
{
    public string Name { get; } = name;

    public WindowSpecificationNode Specification { get; } = specification;

    public override Type ReturnType => typeof(void);

    public override string Id { get; } = $"{nameof(WindowDefinitionNode)}{name}{specification.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name} as {Specification}";
    }
}
