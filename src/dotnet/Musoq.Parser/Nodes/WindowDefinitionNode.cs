using System;

namespace Musoq.Parser.Nodes;

public class WindowDefinitionNode : Node
{
    public WindowDefinitionNode(string name, WindowSpecificationNode specification)
    {
        Name = name;
        Specification = specification;
        Id = $"{nameof(WindowDefinitionNode)}{name}{specification.Id}";
    }

    public string Name { get; }

    public WindowSpecificationNode Specification { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Name} as {Specification}";
    }
}
