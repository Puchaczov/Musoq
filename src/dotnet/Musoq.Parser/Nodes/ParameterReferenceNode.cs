namespace Musoq.Parser.Nodes;

public class ParameterReferenceNode : Node
{
    public ParameterReferenceNode(string name, Type? returnType = null)
        : this(name, returnType, default)
    {
    }

    public ParameterReferenceNode(string name, Type? returnType, TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ReturnType = returnType;
        Id = $"{nameof(ParameterReferenceNode)}{Name}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public override Type? ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"${Name}";
    }
}
