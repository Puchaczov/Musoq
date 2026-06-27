namespace Musoq.Parser.Nodes;

public sealed class ScriptVariableReferenceNode : Node
{
    public ScriptVariableReferenceNode(string name, Type returnType)
        : this(name, returnType, default)
    {
    }

    public ScriptVariableReferenceNode(string name, Type returnType, TextSpan span)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
        Id = $"{nameof(ScriptVariableReferenceNode)}{Name}";
        Span = span;
        FullSpan = span;
    }

    public string Name { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"${Name}";
    }
}